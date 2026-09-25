using System;
using System.Collections.Generic;
using UnityEngine;
using Basket.Core;

namespace Basket.Gameplay
{
    // The per-tick match loop for any number of players on two teams:
    // snapshot -> each controller decides a PlayerCommand -> commands are applied through
    // the motor and ball systems -> loose-ball pickup -> rules tick.
    // Owns no Unity lifecycle; whoever composes the match calls Tick(dt).
    public sealed class MatchSimulation : IDisposable
    {
        private const float CourtEdgeMargin = 0.5f;

        private readonly PlayerEntity[] players;
        private readonly IAgentController[] controllers;
        private readonly BallController ball;
        private readonly CourtConfig court;
        private readonly PassSystem passSystem;
        private readonly ShootingSystem shootingSystem;
        private readonly DribbleSystem dribbleSystem;
        private readonly MatchSnapshot snapshot;
        private float time;

        public MatchManager Match { get; }
        public MatchSnapshot Snapshot => snapshot;
        public IReadOnlyList<PlayerEntity> Players => players;
        public BallController Ball => ball;

        public MatchSimulation(IReadOnlyList<PlayerEntity> players, IReadOnlyList<IAgentController> controllers,
            BallController ball, HoopController hoop, CourtConfig court, MatchRules rules,
            BallConfig ballConfig, ShotConfig shotConfig, System.Random random = null)
        {
            if (players.Count != controllers.Count)
                throw new ArgumentException("Every player needs exactly one controller.");

            this.players = new PlayerEntity[players.Count];
            this.controllers = new IAgentController[controllers.Count];
            for (int i = 0; i < players.Count; i++)
            {
                this.players[i] = players[i];
                this.controllers[i] = controllers[i];
                players[i].Initialize(i, players[i].Team);
            }

            this.ball = ball;
            this.court = court;
            passSystem = new PassSystem(ball, ballConfig);
            shootingSystem = new ShootingSystem(ball, shotConfig, hoop.RimCenter, random);
            dribbleSystem = new DribbleSystem(ball, ballConfig);

            snapshot = new MatchSnapshot(players.Count);
            // Half court: both teams attack the single hoop.
            snapshot.SetAttackingHoop(TeamId.Home, hoop.RimCenter);
            snapshot.SetAttackingHoop(TeamId.Away, hoop.RimCenter);

            Match = new MatchManager(rules, hoop.RimCenter);
            ball.OnScored += Match.HandleScore;
            Match.OnPossessionRestart += SetUpPossession;
        }

        public void Begin() => Match.BeginMatch();

        public void Tick(float dt)
        {
            time += dt;
            RefreshSnapshot();

            MatchPhase phase = Match.State.Phase;
            bool live = phase == MatchPhase.Live;
            for (int i = 0; i < players.Length; i++)
            {
                PlayerCommand command = phase == MatchPhase.Ended ? PlayerCommand.None : controllers[i].Decide(snapshot, i);
                ApplyCommand(i, command, live, dt);
            }

            if (live)
            {
                CatchLooseBall();
                if (ball.CurrentState != BallState.Held && IsOutOfPlay(ball.Position))
                {
                    Match.HandleBallOutOfPlay(ball.LastTouchTeam);
                }
            }
            Match.Tick(dt);
        }

        private void ApplyCommand(int index, PlayerCommand command, bool live, float dt)
        {
            PlayerEntity player = players[index];
            player.Motor.Tick(command.Move, command.Sprint, dt);

            if (ball.CurrentHolder != player.transform) return;
            dribbleSystem.Tick(command.Move.sqrMagnitude > 0.01f, dt);

            if (!live) return;
            if (command.Shoot)
            {
                shootingSystem.TryShoot(player.transform);
            }
            else if (command.Pass)
            {
                int target = PassTargeting.SelectTarget(snapshot, index, command.Move);
                if (target >= 0) passSystem.TryPass(player.transform, players[target].transform);
            }
        }

        private bool IsOutOfPlay(Vector3 p)
        {
            const float slack = 1f;
            return p.y < -1f
                   || Mathf.Abs(p.x) > court.width * 0.5f + slack
                   || p.z < -slack || p.z > court.depth + slack;
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

        private void RefreshSnapshot()
        {
            int holder = -1;
            for (int i = 0; i < players.Length; i++)
            {
                PlayerEntity p = players[i];
                snapshot.SetPlayer(i, p.Team, p.FeetPosition, p.Motor.Velocity);
                if (ball.CurrentHolder == p.transform) holder = i;
            }
            snapshot.SetBall(ball.Position, ball.CurrentState, holder);
            snapshot.SetMatch(Match.State.Phase, time);
        }

        // Check ball (simplified): offense lines up around the arc with the handler at the
        // check spot, each defender between their man and the hoop, handler gets the ball.
        private void SetUpPossession(TeamId offenseTeam)
        {
            var offense = new List<PlayerEntity>();
            var defense = new List<PlayerEntity>();
            foreach (PlayerEntity p in players)
            {
                (p.Team == offenseTeam ? offense : defense).Add(p);
            }
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
                offenseSpots[k] = PossessionLayout.ClampToCourt(
                    PossessionLayout.OffenseSpot(hoopFloor, court.checkBallSpot, k, court.supportPlayerSpreadDegrees),
                    court.width, court.depth, CourtEdgeMargin);
            }

            for (int k = 0; k < offense.Count; k++)
            {
                offense[k].TeleportFeetTo(offenseSpots[k], hoopFloor - offenseSpots[k]);
            }
            for (int k = 0; k < defense.Count; k++)
            {
                Vector3 spot = PossessionLayout.DefenseSpot(hoopFloor, offenseSpots[k], court.defenderGap);
                defense[k].TeleportFeetTo(spot, offenseSpots[k] - spot);
            }

            ball.ResetToHolder(offense[0].transform);
        }

        public void Dispose()
        {
            ball.OnScored -= Match.HandleScore;
            Match.OnPossessionRestart -= SetUpPossession;
        }
    }
}
