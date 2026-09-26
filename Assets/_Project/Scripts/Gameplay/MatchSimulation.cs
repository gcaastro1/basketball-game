using System;
using System.Collections.Generic;
using UnityEngine;
using Basket.Core;
using Basket.Characters;

namespace Basket.Gameplay
{
    // The per-tick match loop for any number of players on two teams:
    // snapshot -> each controller decides a PlayerCommand -> commands are applied through
    // the motor, shot, pass and defense systems -> blocks / loose-ball pickup -> facts
    // reported to the referee (MatchManager), which answers with restarts and free throws.
    // Owns no Unity lifecycle; whoever composes the match calls Tick(dt).
    public sealed class MatchSimulation : IDisposable, IMatchEventFeed
    {
        private const float CourtEdgeMargin = 0.5f;

        private readonly PlayerEntity[] players;
        private readonly IAgentController[] controllers;
        private readonly BallController ball;
        private readonly CourtConfig court;
        private readonly MatchRules rules;
        private readonly BallConfig ballConfig;
        private readonly DefenseConfig defenseConfig;
        private readonly System.Random rng;
        private readonly PassSystem passSystem;
        private readonly ShotSystem shotSystem;
        private readonly DefenseSystem defenseSystem;
        private readonly DribbleSystem dribbleSystem;
        private readonly MatchSnapshot snapshot;
        private readonly AttributeTuning tuning;
        private readonly int[] consecutiveMakes;
        private int pendingShooter = -1;
        private readonly HoopController homeAttacks;
        private readonly HoopController awayAttacks;
        private TeamId? lastTouchBeforeTick;
        private int lastHolderIndex = -1;
        private float time;
        private bool awaitingRebound;
        private bool awaitingPass;
        private TeamId passTeam;
        private int passerIndex = -1;
        private int passTargetIndex = -1;
        private TeamId reboundShooterTeam;

        public MatchManager Match { get; }
        public MatchSnapshot Snapshot => snapshot;
        public IReadOnlyList<PlayerEntity> Players => players;
        public BallController Ball => ball;
        public IShotReportSource ShotReports => shotSystem;
        public MatchStats Stats { get; } = new MatchStats();
        public event Action<string> OnMatchEvent;
        // Presentation hooks (read-only views of gameplay; nothing here changes the rules).
        public event Action<int> OnPassThrown;
        public float SimTime => time;
        public bool TryGetShot(int index, out ShotType type, out float progress) =>
            shotSystem.TryGetShot(index, time, out type, out progress);

        public MatchSimulation(IReadOnlyList<PlayerEntity> players, IReadOnlyList<IAgentController> controllers,
            BallController ball, HoopController hoop, CourtConfig court, MatchRules rules,
            BallConfig ballConfig, ShotConfig shotConfig, DefenseConfig defenseConfig = null, System.Random random = null,
            AttributeTuning attributeTuning = null, HoopController secondHoop = null)
        {
            tuning = attributeTuning != null ? attributeTuning : ScriptableObject.CreateInstance<AttributeTuning>();
            consecutiveMakes = new int[players.Count];
            if (players.Count != controllers.Count)
                throw new ArgumentException("Every player needs exactly one controller.");

            this.players = new PlayerEntity[players.Count];
            this.controllers = new IAgentController[controllers.Count];
            for (int i = 0; i < players.Count; i++)
            {
                this.players[i] = players[i];
                this.controllers[i] = controllers[i];
                players[i].Initialize(i, players[i].Team);
                players[i].SetTuning(tuning);
            }

            this.ball = ball;
            this.court = court;
            this.rules = rules;
            this.ballConfig = ballConfig;
            this.defenseConfig = defenseConfig != null ? defenseConfig : ScriptableObject.CreateInstance<DefenseConfig>();
            rng = random ?? new System.Random();
            passSystem = new PassSystem(ball, ballConfig);
            shotSystem = new ShotSystem(players.Count, ball, shotConfig, ballConfig, rng, tuning, rules.threePointRadius);
            defenseSystem = new DefenseSystem(players.Count, ball, this.defenseConfig, rng, tuning);
            dribbleSystem = new DribbleSystem(ball, ballConfig);

            snapshot = new MatchSnapshot(players.Count);
            // Half court: both teams attack the single hoop. Full court: Home attacks the
            // first basket, Away the mirrored one, until the teams switch sides.
            homeAttacks = hoop;
            awayAttacks = secondHoop != null ? secondHoop : hoop;
            AssignBaskets();
            snapshot.SetThreePointRadius(rules.threePointRadius);
            snapshot.SetScoring(rules.pointsInsideArc, rules.pointsBeyondArc);
            snapshot.SetCourtCenter(court.CourtCenter);

            Match = new MatchManager(rules, hoop.RimCenter);
            ball.OnScored += Match.HandleScore;
            ball.OnRimTouched += Match.NotifyRimTouched;
            Match.OnPossessionRestart += SetUpPossession;
            Match.OnFreeThrowSetup += SetUpFreeThrow;
            Match.OnSidesSwitched += SwitchSides;
            Match.OnRuleEvent += Raise;
            Match.OnBasketCounted += CountBasket;
            Match.OnFoulCalled += CountFoul;
            Match.OnTurnover += CountTurnover;
            ball.OnShotTraced += Raise;
            shotSystem.OnShotTaken += OnShotTaken;
            defenseSystem.OnSteal += OnSteal;
            defenseSystem.OnBlock += OnBlock;
        }

        public void Begin() => Match.BeginMatch();

        public void Tick(float dt)
        {
            time += dt;
            lastTouchBeforeTick = ball.LastTouchTeam;
            RefreshSnapshot();

            MatchPhase phase = Match.State.Phase;
            bool live = phase == MatchPhase.Live;
            int freeThrowShooter = Match.FreeThrowShooter;
            shotSystem.SetFreeThrowShooter(freeThrowShooter);

            for (int i = 0; i < players.Length; i++)
            {
                PlayerCommand command = CommandFor(i, phase, freeThrowShooter);
                ApplyCommand(i, command, live, phase == MatchPhase.FreeThrow && i == freeThrowShooter, dt);
            }

            if (Match.State.Phase == MatchPhase.Live)
            {
                defenseSystem.CheckBlocks(players, ball.LastTouchTeam);
                CatchLooseBall();
                ReportPossession();
                CheckBoundaries();
            }

            BallState state = ball.CurrentState;
            Match.Tick(dt, new BallStatus(
                possessed: state == BallState.Held || state == BallState.Passing,
                shotInFlight: state == BallState.Shooting,
                liveRelease: ball.HasLiveRelease));
        }

        private PlayerCommand CommandFor(int index, MatchPhase phase, int freeThrowShooter)
        {
            switch (phase)
            {
                case MatchPhase.Ended:
                    return PlayerCommand.None;
                case MatchPhase.FreeThrow:
                    if (index != freeThrowShooter) return PlayerCommand.None;
                    // The shooter stays on the line: only the shoot button counts.
                    PlayerCommand c = controllers[index].Decide(snapshot, index);
                    return new PlayerCommand(Vector2.zero, shootHeld: c.ShootHeld);
                default:
                    return controllers[index].Decide(snapshot, index);
            }
        }

        private void ApplyCommand(int index, PlayerCommand command, bool live, bool freeThrow, float dt)
        {
            PlayerEntity player = players[index];
            bool holding = ball.CurrentHolder == player.transform;
            UpdateCharacter(index, player, command, holding, live, dt);
            // Shooters square up to the basket; a defender in guard faces the ball, slides slower
            // and cannot sprint.
            bool guarding = command.Guard && !holding && live;
            player.IsGuarding = guarding;
            Vector3? face = null;
            float speedMultiplier = 1f;
            bool sprint = command.Sprint;
            if (shotSystem.IsShooting(index))
            {
                face = snapshot.GetAttackingHoop(player.Team);
            }
            else if (guarding)
            {
                face = ball.Position;
                speedMultiplier = player.Motor.Config.guardSpeedMultiplier;
                sprint = false;
            }
            player.Motor.Tick(command.Move, sprint, dt, face, speedMultiplier);

            if (live || freeThrow) shotSystem.Tick(index, player, command, snapshot, time);
            if (shotSystem.IsShooting(index)) return;

            if (holding)
            {
                dribbleSystem.Tick(command.Move.sqrMagnitude > 0.01f, dt);
                if (live && command.Pass)
                {
                    int target = PassTargeting.SelectTarget(snapshot, index, command.Move);
                    if (target >= 0 && passSystem.TryPass(player.transform, players[target].transform))
                    {
                        Stats.Get(player.Team).Passes++;
                        awaitingPass = true;
                        passTeam = player.Team;
                        passerIndex = index;
                        passTargetIndex = target;
                        OnPassThrown?.Invoke(index);
                    }
                }
                return;
            }
            if (!live) return;

            // Jumping with the ball without shooting would be a travel; only off-ball jumps.
            if (command.Jump && player.Motor.IsGrounded && player.Motor.Jump()) player.Stamina -= tuning.jumpCost;
            if (command.Steal) TrySteal(index);
        }

        // Attributes, stamina and abilities -> this tick's motor scales and ability state.
        private void UpdateCharacter(int index, PlayerEntity player, PlayerCommand command, bool holding, bool live, float dt)
        {
            bool moving = command.Move.sqrMagnitude > 0.01f;
            float drain = command.Sprint && moving
                ? tuning.sprintDrainPerSecond * player.AttributeMult(AttributeId.Stamina, tuning.staminaDrain)
                : -tuning.recoveryPerSecond;
            player.Stamina = Mathf.Clamp01(player.Stamina - drain * dt);

            if (player.Abilities != null)
            {
                MatchState s = Match.State;
                int diff = s.GetScore(player.Team) - s.GetScore(player.Team.Opponent());
                player.Abilities.Tick(new AbilityContext(time, s.GameClock, diff, consecutiveMakes[index]));
                if (live && command.Ability)
                {
                    AbilityDefinition used = player.Abilities.TryActivate(time);
                    if (used != null) Raise($"ABILITY {used.displayName} by {player.name}");
                }
            }

            float tired = Mathf.Lerp(1f, tuning.tiredSpeedAtEmpty, tuning.Tiredness(player.Stamina));
            float handling = holding ? player.AttributeMult(AttributeId.BallHandling, tuning.handlingSpeed) : 1f;
            player.Motor.SetScales(
                player.AttributeMult(AttributeId.Speed, tuning.speed) * tired * handling,
                player.AttributeMult(AttributeId.Acceleration, tuning.acceleration),
                player.AttributeMult(AttributeId.Agility, tuning.agilityTurn),
                player.AttributeMult(AttributeId.Vertical, tuning.verticalJump));
        }

        private void TrySteal(int index)
        {
            int holder = snapshot.BallHolderIndex;
            StealOutcome outcome = defenseSystem.TrySteal(index, players, holder >= 0 && shotSystem.IsShooting(holder), time);
            if (outcome == StealOutcome.Missed && holder >= 0 && rng.NextDouble() < defenseConfig.reachInFoulChance)
            {
                Raise($"Reach-in by {players[index].name}");
                Match.HandleFoul(new FoulEvent(players[index].Team, holder, players[holder].Team, shooting: false));
            }
        }

        // Nearest eligible player wins a loose ball, independent of roster order.
        private void CatchLooseBall()
        {
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < players.Length; i++)
            {
                Transform t = players[i].transform;
                if (!ball.CanBeCaughtBy(t)) continue;
                float distance = ball.DistanceTo(t);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }
            if (best >= 0) ball.TryCatchNearby(players[best].transform);
        }

        private void ReportPossession()
        {
            if (ball.CurrentState != BallState.Held)
            {
                lastHolderIndex = -1;
                return;
            }
            for (int i = 0; i < players.Length; i++)
            {
                if (ball.CurrentHolder != players[i].transform) continue;
                bool newCatch = i != lastHolderIndex;
                lastHolderIndex = i;
                if (awaitingPass)
                {
                    awaitingPass = false;
                    if (players[i].Team == passTeam) Stats.Get(passTeam).PassesCompleted++;
                    else
                    {
                        Stats.Get(passTeam).Turnovers++; // intercepted / thrown away
                        if (passerIndex >= 0 && passTargetIndex >= 0)
                        {
                            Vector3 from = players[passerIndex].FeetPosition, at = players[i].FeetPosition;
                            Raise($"PASS LOST {players[passerIndex].name}->{players[passTargetIndex].name}: caught by {players[i].name} " +
                                  $"{Vector3.Distance(from, at):0.0} m from the passer, receiver was {Vector3.Distance(from, players[passTargetIndex].FeetPosition):0.0} m away; " +
                                  $"ball {(ball.PassWentLoose ? "went loose off " + ball.PassLooseCause : "intercepted in flight")}, closest to receiver {ball.PassClosestToReceiver:0.0} m");
                        }
                    }
                }
                if (awaitingRebound)
                {
                    awaitingRebound = false;
                    if (pendingShooter >= 0)
                    {
                        consecutiveMakes[pendingShooter] = 0; // missed
                        pendingShooter = -1;
                    }
                    TeamStats stats = Stats.Get(players[i].Team);
                    if (players[i].Team == reboundShooterTeam) stats.OffensiveRebounds++;
                    else stats.DefensiveRebounds++;
                }
                bool byOpponent = newCatch && lastTouchBeforeTick.HasValue && lastTouchBeforeTick.Value != players[i].Team;
                Match.NotifyPossession(players[i].Team, IsBeyondArc(players[i].FeetPosition, players[i].Team),
                    IsInFrontcourt(players[i].FeetPosition, players[i].Team), byOpponent);
                return;
            }
        }

        private bool IsBeyondArc(Vector3 feet, TeamId team)
        {
            Vector3 hoop = snapshot.GetAttackingHoop(team);
            return ScoringMath.IsBeyondArc(feet, hoop, rules.threePointRadius, rules.threePointCornerDistance);
        }

        // The half of the court with the basket this team attacks (always true on a half court).
        private bool IsInFrontcourt(Vector3 feet, TeamId team)
        {
            if (!court.fullCourt) return true;
            float hoopSide = snapshot.GetAttackingHoop(team).z - court.CourtCenter.z;
            return (feet.z - court.CourtCenter.z) * hoopSide >= 0f;
        }

        private bool IsOutsideLines(Vector3 p) =>
            Mathf.Abs(p.x) > court.width * 0.5f || p.z < 0f || p.z > court.depth;

        // Ball on/over the floor outside the lines, or a ball handler stepping out.
        private void CheckBoundaries()
        {
            if (ball.CurrentState != BallState.Held)
            {
                bool outOfBounds = rules.useBoundaryLines && IsOutsideLines(ball.Position) && ball.Position.y < 1f;
                if (outOfBounds || IsOutOfPlay(ball.Position)) Match.HandleBallOutOfPlay(ball.LastTouchTeam);
                return;
            }
            if (!rules.useBoundaryLines || lastHolderIndex < 0) return;
            PlayerEntity holder = players[lastHolderIndex];
            if (IsOutsideLines(holder.FeetPosition)) Match.HandleBallOutOfPlay(holder.Team);
        }

        private void AssignBaskets()
        {
            snapshot.SetAttackingHoop(TeamId.Home, homeAttacks.RimCenter);
            snapshot.SetAttackingHoop(TeamId.Away, awayAttacks.RimCenter);
            if (homeAttacks != awayAttacks)
            {
                homeAttacks.AttackingTeam = TeamId.Home;
                awayAttacks.AttackingTeam = TeamId.Away;
            }
        }

        private void SwitchSides()
        {
            if (homeAttacks == awayAttacks) return;
            HoopController a = homeAttacks.AttackingTeam == TeamId.Home ? homeAttacks : awayAttacks;
            HoopController b = a == homeAttacks ? awayAttacks : homeAttacks;
            a.AttackingTeam = TeamId.Away;
            b.AttackingTeam = TeamId.Home;
            snapshot.SetAttackingHoop(TeamId.Home, b.RimCenter);
            snapshot.SetAttackingHoop(TeamId.Away, a.RimCenter);
        }

        private bool IsOutOfPlay(Vector3 p)
        {
            const float slack = 1f;
            return p.y < -1f
                   || Mathf.Abs(p.x) > court.width * 0.5f + slack
                   || p.z < -slack || p.z > court.depth + slack;
        }

        private void RefreshSnapshot()
        {
            int holder = -1;
            for (int i = 0; i < players.Length; i++)
            {
                PlayerEntity p = players[i];
                snapshot.SetPlayer(i, p.Team, p.FeetPosition, p.Motor.Velocity, p.Motor.IsGrounded);
                snapshot.SetAttributes(i, p.Attributes);
                snapshot.SetAbilityReady(i, p.Abilities != null && p.Abilities.CanActivate(time));
                if (ball.CurrentHolder == p.transform) holder = i;
            }
            snapshot.SetBall(ball.Position, ball.CurrentState, holder, ball.Velocity);
            int passTarget = -1;
            if (ball.CurrentState == BallState.Passing && ball.PassReceiver != null)
            {
                for (int i = 0; i < players.Length; i++)
                {
                    if (players[i].transform == ball.PassReceiver) passTarget = i;
                }
            }
            snapshot.SetPassTarget(passTarget);
            MatchState s = Match.State;
            snapshot.SetMatch(s.Phase, time, s.ShotClock, s.BallMustBeCleared);
        }

        // ---------- shots, steals, blocks ----------

        private void OnShotTaken(ShotReport r)
        {
            PlayerEntity shooter = players[r.ShooterIndex];
            bool beyondArc = IsBeyondArc(shooter.FeetPosition, shooter.Team);
            Match.NotifyShotReleased(r.ShooterIndex, beyondArc);
            ReportShot(r);

            TeamStats stats = Stats.Get(shooter.Team);
            if (r.Type == ShotType.FreeThrow)
            {
                stats.FreeThrowsAttempted++;
            }
            else
            {
                stats.FieldGoalsAttempted++;
                if (beyondArc) stats.ThreesAttempted++;
                lastShotBeyondArc = beyondArc;
            }
            awaitingRebound = true;
            awaitingPass = false;
            reboundShooterTeam = shooter.Team;
            if (r.Type != ShotType.FreeThrow)
            {
                // Previous shot by this player that never went in breaks the streak.
                if (pendingShooter >= 0) consecutiveMakes[pendingShooter] = 0;
                pendingShooter = r.ShooterIndex;
            }

            if (r.Type == ShotType.FreeThrow || Match.State.Phase != MatchPhase.Live) return;
            int fouler = FoulMath.ShootingContact(snapshot, r.ShooterIndex,
                defenseConfig.shootingContactDistance, defenseConfig.shootingContactClosingSpeed);
            if (fouler >= 0 && rng.NextDouble() < defenseConfig.shootingFoulChance)
            {
                Match.HandleFoul(new FoulEvent(players[fouler].Team, r.ShooterIndex, shooter.Team, shooting: true));
            }
        }

        private bool lastShotBeyondArc;

        private void OnSteal(int i)
        {
            Stats.Get(players[i].Team).Steals++;
            Stats.Get(players[i].Team.Opponent()).Turnovers++;
            Raise($"STEAL by {players[i].name}");
        }

        private void OnBlock(int i)
        {
            Stats.Get(players[i].Team).Blocks++;
            Raise($"BLOCK by {players[i].name}");
        }

        private void CountBasket(TeamId team, int points, ShotType? type)
        {
            awaitingRebound = false;
            if (type != ShotType.FreeThrow && pendingShooter >= 0 && players[pendingShooter].Team == team)
            {
                consecutiveMakes[pendingShooter]++;
                pendingShooter = -1;
            }
            TeamStats stats = Stats.Get(team);
            if (type == ShotType.FreeThrow)
            {
                stats.FreeThrowsMade++;
                return;
            }
            stats.FieldGoalsMade++;
            if (lastShotBeyondArc) stats.ThreesMade++;
        }

        private void CountFoul(FoulEvent foul) => Stats.Get(foul.FoulerTeam).Fouls++;
        private void CountTurnover(TeamId team) => Stats.Get(team).Turnovers++;

        // ---------- restarts ----------

        private void SplitTeams(TeamId offenseTeam, out List<PlayerEntity> offense, out List<PlayerEntity> defense)
        {
            offense = new List<PlayerEntity>();
            defense = new List<PlayerEntity>();
            foreach (PlayerEntity p in players)
            {
                (p.Team == offenseTeam ? offense : defense).Add(p);
            }
        }

        // Offense lines up around the arc (handler at the check spot, or under the basket
        // for an inbound that must be cleared); each defender takes the attacker with the
        // same rank as their man-to-man matchup.
        private void SetUpPossession(TeamId offenseTeam, RestartKind kind)
        {
            awaitingRebound = false;
            awaitingPass = false;
            lastHolderIndex = -1;
            switch (kind)
            {
                case RestartKind.JumpBall:
                    SetUpJumpBall();
                    return;
                case RestartKind.BaselineInbound:
                case RestartKind.SidelineInbound:
                case RestartKind.MidcourtInbound:
                    SetUpInbound(offenseTeam, kind);
                    return;
            }
            SetUpHalfCourtLineup(offenseTeam, kind);
        }

        // Full court: only the inbounder is placed; everyone else keeps playing from
        // where they are (that is what makes transition happen).
        private void SetUpInbound(TeamId offenseTeam, RestartKind kind)
        {
            SplitTeams(offenseTeam, out var offense, out var defense);
            if (offense.Count == 0) (offense, defense) = (defense, offense);
            if (offense.Count == 0) return;

            Vector3 center = court.CourtCenter;
            // The placeholder arena's walls stand on the lines: the inbounder stays far enough
            // inside that the ball leaves their hands clear of the wall (at 0.3 m inbound
            // passes hit the wall on release -- AI-vs-AI log).
            const float inboundMargin = 0.8f;
            float sideX = (court.width * 0.5f - inboundMargin) * (ball.Position.x >= 0f ? 1f : -1f);
            Vector3 spot;
            switch (kind)
            {
                case RestartKind.BaselineInbound:
                    // Under the basket that was just scored on (the one this team defends).
                    Vector3 hoop = snapshot.GetDefendedHoop(offense[0].Team);
                    Vector3 hoopFloor = new Vector3(hoop.x, 0f, hoop.z);
                    Vector3 towardBaseline = (hoopFloor - center).normalized;
                    spot = Clamp(hoopFloor + towardBaseline * 0.9f, inboundMargin);
                    break;
                case RestartKind.MidcourtInbound:
                    spot = new Vector3(sideX, 0f, center.z);
                    break;
                default:
                    spot = new Vector3(sideX, 0f, Mathf.Clamp(ball.Position.z, 1f, court.depth - 1f));
                    break;
            }

            PlayerEntity inbounder = Nearest(offense, spot);
            Vector3 attackHoop = snapshot.GetAttackingHoop(inbounder.Team);
            inbounder.TeleportFeetTo(spot, new Vector3(attackHoop.x, 0f, attackHoop.z) - spot);
            AssignMatchupsByProximity(offense, defense);
            shotSystem.ResetAll();
            ball.ResetToHolder(inbounder.transform);
        }

        // Jump ball at the center circle: one jumper per team, the rest in their own half.
        private void SetUpJumpBall()
        {
            SplitTeams(TeamId.Home, out var home, out var away);
            Vector3 center = court.CourtCenter;
            PlaceJumpTeam(home, center);
            PlaceJumpTeam(away, center);
            AssignMatchupsByProximity(home, away);
            shotSystem.ResetAll();
            // Tossed from above everyone's standing reach: only a jumper can get to it (from
            // 2.2 m a jumper standing next to it caught it on the first tick).
            float reach = 0f;
            foreach (PlayerEntity p in players) reach = Mathf.Max(reach, p.StandingReach);
            ball.Toss(center + Vector3.up * (reach + ballConfig.catchReachMargin + 0.3f), Vector3.up * 3f);
        }

        private void PlaceJumpTeam(List<PlayerEntity> team, Vector3 center)
        {
            if (team.Count == 0) return;
            Vector3 defended = snapshot.GetDefendedHoop(team[0].Team);
            Vector3 back = new Vector3(defended.x - center.x, 0f, defended.z - center.z);
            back = back.sqrMagnitude < 0.0001f ? Vector3.back : back.normalized;

            // Best leaper jumps.
            PlayerEntity jumper = team[0];
            foreach (PlayerEntity p in team)
            {
                if (p.AttributeMult(AttributeId.Vertical, tuning.verticalJump) > jumper.AttributeMult(AttributeId.Vertical, tuning.verticalJump)) jumper = p;
            }
            jumper.TeleportFeetTo(center + back * 0.6f, -back);

            int k = 0;
            foreach (PlayerEntity p in team)
            {
                if (p == jumper) continue;
                float angle = (k % 2 == 0 ? 1f : -1f) * (35f + 30f * (k / 2));
                Vector3 spot = center + Quaternion.AngleAxis(angle, Vector3.up) * back * (court.centerCircleRadius + 1.5f);
                p.TeleportFeetTo(Clamp(spot, 0.5f), center - spot);
                k++;
            }
        }

        private static PlayerEntity Nearest(List<PlayerEntity> candidates, Vector3 point)
        {
            PlayerEntity best = candidates[0];
            float bestDistance = float.MaxValue;
            foreach (PlayerEntity p in candidates)
            {
                Vector3 d = p.FeetPosition - point;
                d.y = 0f;
                if (d.sqrMagnitude < bestDistance)
                {
                    bestDistance = d.sqrMagnitude;
                    best = p;
                }
            }
            return best;
        }

        // Greedy nearest pairs: players are scattered after a live-ball restart.
        private void AssignMatchupsByProximity(List<PlayerEntity> offense, List<PlayerEntity> defense)
        {
            for (int i = 0; i < players.Length; i++) snapshot.SetMatchup(i, -1);
            var freeDefenders = new List<PlayerEntity>(defense);
            var freeAttackers = new List<PlayerEntity>(offense);
            while (freeDefenders.Count > 0 && freeAttackers.Count > 0)
            {
                PlayerEntity bestD = null, bestA = null;
                float best = float.MaxValue;
                foreach (PlayerEntity d in freeDefenders)
                {
                    foreach (PlayerEntity a in freeAttackers)
                    {
                        Vector3 v = d.FeetPosition - a.FeetPosition;
                        v.y = 0f;
                        if (v.sqrMagnitude < best)
                        {
                            best = v.sqrMagnitude;
                            bestD = d;
                            bestA = a;
                        }
                    }
                }
                snapshot.SetMatchup(bestA.Index, bestD.Index);
                snapshot.SetMatchup(bestD.Index, bestA.Index);
                freeDefenders.Remove(bestD);
                freeAttackers.Remove(bestA);
            }
        }

        private void SetUpHalfCourtLineup(TeamId offenseTeam, RestartKind kind)
        {
            SplitTeams(offenseTeam, out var offense, out var defense);
            if (offense.Count == 0)
            {
                // e.g. a solo shooting drill: the only team present keeps the ball.
                (offense, defense) = (defense, offense);
            }
            if (offense.Count == 0) return;

            Vector3 hoopFloor = court.RimFloorProjection;
            var offenseSpots = new Vector3[Mathf.Max(offense.Count, defense.Count)];
            for (int k = 0; k < offenseSpots.Length; k++)
            {
                offenseSpots[k] = Clamp(PossessionLayout.OffenseSpot(hoopFloor, court.checkBallSpot, k, court.supportPlayerSpreadDegrees));
            }
            if (kind == RestartKind.UnderBasket)
            {
                offenseSpots[0] = Clamp(PossessionLayout.AlongCourtAxis(hoopFloor, court.checkBallSpot, court.underBasketInboundDistance));
            }

            for (int k = 0; k < offense.Count; k++)
            {
                offense[k].TeleportFeetTo(offenseSpots[k], hoopFloor - offenseSpots[k]);
            }
            for (int k = 0; k < defense.Count; k++)
            {
                Vector3 spot = kind == RestartKind.UnderBasket && k == 0
                    // Picks up the inbounder at the arc instead of standing under the rim.
                    ? Clamp(PossessionLayout.AlongCourtAxis(hoopFloor, court.checkBallSpot, rules.threePointRadius - 0.5f))
                    : PossessionLayout.DefenseSpot(hoopFloor, offenseSpots[k], court.defenderGap);
                defense[k].TeleportFeetTo(spot, offenseSpots[k] - spot);
            }

            AssignMatchups(offense, defense);
            awaitingRebound = false;
            awaitingPass = false;
            shotSystem.ResetAll();
            ball.ResetToHolder(offense[0].transform);
        }

        private void SetUpFreeThrow(int shooterIndex)
        {
            PlayerEntity shooter = players[shooterIndex];
            Vector3 hoop = snapshot.GetAttackingHoop(shooter.Team);
            Vector3 hoopFloor = new Vector3(hoop.x, 0f, hoop.z);
            Vector3 line = PossessionLayout.AlongCourtAxis(hoopFloor, court.CourtCenter, court.freeThrowDistance);
            shooter.TeleportFeetTo(line, hoopFloor - line);

            int slot = 0;
            for (int i = 0; i < players.Length; i++)
            {
                if (i == shooterIndex) continue;
                Vector3 spot = Clamp(PossessionLayout.LaneSlot(hoopFloor, court.CourtCenter, slot++));
                players[i].TeleportFeetTo(spot, hoopFloor - spot);
            }
            shotSystem.ResetAll();
            ball.ResetToHolder(shooter.transform);
        }

        private void AssignMatchups(List<PlayerEntity> offense, List<PlayerEntity> defense)
        {
            for (int i = 0; i < players.Length; i++) snapshot.SetMatchup(i, -1);
            int pairs = Mathf.Min(offense.Count, defense.Count);
            for (int k = 0; k < pairs; k++)
            {
                snapshot.SetMatchup(offense[k].Index, defense[k].Index);
                snapshot.SetMatchup(defense[k].Index, offense[k].Index);
            }
        }

        private Vector3 Clamp(Vector3 spot) => Clamp(spot, CourtEdgeMargin);
        private Vector3 Clamp(Vector3 spot, float margin) => PossessionLayout.ClampToCourt(spot, court.width, court.depth, margin);

        private void ReportShot(ShotReport r)
        {
            bool timed = r.Type == ShotType.JumpShot || r.Type == ShotType.FreeThrow;
            string timing = !timed ? "auto"
                : Mathf.Abs(r.TimingError) <= 0.05f ? "PERFECT"
                : r.TimingError < 0f ? $"early {-r.TimingError:0.00}s" : $"late {r.TimingError:0.00}s";
            Raise($"{r.Type} by {players[r.ShooterIndex].name}: {r.Distance:0.0} m, {timing}, contest {r.Contest:0.00}, error {r.ErrorRadius:0.00} m");
        }

        private void Raise(string message) => OnMatchEvent?.Invoke(message);

        public void Dispose()
        {
            ball.OnScored -= Match.HandleScore;
            ball.OnRimTouched -= Match.NotifyRimTouched;
            Match.OnPossessionRestart -= SetUpPossession;
            Match.OnFreeThrowSetup -= SetUpFreeThrow;
            Match.OnSidesSwitched -= SwitchSides;
            Match.OnRuleEvent -= Raise;
            Match.OnBasketCounted -= CountBasket;
            Match.OnFoulCalled -= CountFoul;
            Match.OnTurnover -= CountTurnover;
            ball.OnShotTraced -= Raise;
            shotSystem.OnShotTaken -= OnShotTaken;
            defenseSystem.OnSteal -= OnSteal;
            defenseSystem.OnBlock -= OnBlock;
        }
    }
}
