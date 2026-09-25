using System;
using UnityEngine;
using Basket.Core;

namespace Basket.Gameplay
{
    // What the ball is doing this tick, as far as the rules care.
    public readonly struct BallStatus
    {
        // Held or in a pass by the team in possession: the shot clock runs.
        public readonly bool Possessed;
        // A shot in the air that has not touched anything yet.
        public readonly bool ShotInFlight;
        // A release that can still score (not yet on the floor / caught).
        public readonly bool LiveRelease;

        public BallStatus(bool possessed, bool shotInFlight, bool liveRelease)
        {
            Possessed = possessed;
            ShotInFlight = shotInFlight;
            LiveRelease = liveRelease;
        }
    }

    // The referee. Receives facts from the simulation (possession, rim touches, shots,
    // baskets, fouls, ball status) and applies MatchRules: score, clocks, clearing,
    // fouls, free throws, periods, overtime. It never touches physics or positions: it
    // asks MatchSimulation to set up possessions and free throws through events.
    public sealed class MatchManager
    {
        private enum Pending { None, Possession, FreeThrow }

        private readonly MatchRules rules;
        private readonly Vector3 rimCenter;
        private readonly GameClock gameClock = new GameClock();
        private readonly ShotClock shotClock = new ShotClock();
        private bool suddenDeath;

        private Pending pending;
        private float pendingTimer;
        private TeamId pendingTeam;
        private RestartKind pendingKind;

        private TeamId? possessionTeam;
        private bool needsClear;
        private bool lastShotBeyondArc;

        private bool shootingFoulPending;
        private FoulEvent shootingFoul;

        private int ftRemaining;
        private int ftShooter = -1;
        private TeamId ftTeam;
        private AfterFreeThrows ftAfter;
        private bool ftAwaitingResult;

        public MatchState State { get; }
        public int FreeThrowShooter => State.Phase == MatchPhase.FreeThrow ? ftShooter : -1;

        public event Action<TeamId, RestartKind> OnPossessionRestart;
        public event Action<int> OnFreeThrowSetup;
        public event Action<string> OnRuleEvent;

        public MatchManager(MatchRules matchRules, Vector3 rimCenter)
        {
            rules = matchRules;
            this.rimCenter = rimCenter;
            State = new MatchState(matchRules.winningScore);
            SyncState();
        }

        public void BeginMatch()
        {
            if (rules.useGameClock) gameClock.StartPeriod(1, rules.periodLengthSeconds, overtime: false);
            RestartPossession(rules.firstPossession, RestartKind.CheckBall);
        }

        // ---------- facts from the simulation ----------

        public void NotifyPossession(TeamId team, bool beyondArc)
        {
            if (possessionTeam != team)
            {
                bool liveChange = State.Phase == MatchPhase.Live && possessionTeam.HasValue;
                possessionTeam = team;
                shotClock.Reset(rules.shotClockSeconds);
                needsClear = liveChange && rules.clearBallOnChangeOfPossession;
            }
            if (needsClear && beyondArc)
            {
                needsClear = false;
                Raise("Ball cleared");
            }
            SyncState();
        }

        public void NotifyRimTouched()
        {
            shotClock.Reset(rules.shotClockAfterRimTouch);
            SyncState();
        }

        public void NotifyShotReleased(int shooterIndex, bool beyondArc)
        {
            if (State.Phase == MatchPhase.FreeThrow && shooterIndex == ftShooter)
            {
                ftAwaitingResult = true;
                return;
            }
            lastShotBeyondArc = beyondArc;
        }

        public void HandleFoul(FoulEvent foul)
        {
            if (State.Phase != MatchPhase.Live) return;
            int fouls = State.AddTeamFoul(foul.FoulerTeam);
            Raise($"FOUL on {foul.FoulerTeam}{(foul.Shooting ? " (shooting)" : "")} - team fouls {fouls}");

            if (foul.Shooting)
            {
                // Free throws depend on whether the shot goes in: resolved later.
                shootingFoulPending = true;
                shootingFoul = foul;
                return;
            }

            shootingFoulPending = false;
            State.ResetForNextPossession();
            AwardFreeThrowsOrBall(foul.FouledIndex, foul.FouledTeam, FoulRules.NonShooting(rules, fouls));
        }

        public void HandleScore(ScoreEvent scoreEvent)
        {
            if (scoreEvent.Team == null) return;
            TeamId team = scoreEvent.Team.Value;

            if (scoreEvent.ShotType == ShotType.FreeThrow)
            {
                HandleFreeThrowMade(team);
                return;
            }
            if (State.Phase != MatchPhase.Live) return;

            if (needsClear && possessionTeam == team)
            {
                Raise("Basket does not count: the ball was not cleared");
                shootingFoulPending = false;
                State.ResetForNextPossession();
                Schedule(Pending.Possession, team.Opponent(), RestartKind.CheckBall);
                return;
            }

            int points = ScoringMath.PointsForRelease(scoreEvent.ReleasePosition, rimCenter,
                rules.threePointRadius, rules.pointsInsideArc, rules.pointsBeyondArc);
            State.RegisterScore(team, points);
            if (State.Phase == MatchPhase.Ended) return;

            if (shootingFoulPending && shootingFoul.FouledTeam == team)
            {
                shootingFoulPending = false;
                FoulPenalty andOne = FoulRules.Shooting(rules, State.GetTeamFouls(shootingFoul.FoulerTeam), shotScored: true, lastShotBeyondArc);
                if (andOne.FreeThrows > 0)
                {
                    StartFreeThrows(shootingFoul.FouledIndex, team, andOne);
                    return;
                }
            }
            Schedule(Pending.Possession, team.Opponent(), rules.afterMadeBasket);
        }

        // Ball left the play area (e.g. over the placeholder walls). Provisional rule: the
        // other team from whoever touched it last gets a check ball.
        public void HandleBallOutOfPlay(TeamId? lastTouchTeam)
        {
            if (State.Phase != MatchPhase.Live) return;
            if (shootingFoulPending)
            {
                ResolveShootingFoulMissed();
                return;
            }
            State.ResetForNextPossession();
            Schedule(Pending.Possession, lastTouchTeam.HasValue ? lastTouchTeam.Value.Opponent() : rules.firstPossession, RestartKind.CheckBall);
        }

        public void Tick(float dt) => Tick(dt, default);

        public void Tick(float dt, BallStatus ball)
        {
            if (pending != Pending.None)
            {
                pendingTimer -= dt;
                if (pendingTimer <= 0f) ExecutePending();
            }

            if (State.Phase == MatchPhase.Live) TickLive(dt, ball);
            else if (State.Phase == MatchPhase.FreeThrow) TickFreeThrow(ball);
            SyncState();
        }

        // ---------- live play ----------

        private void TickLive(float dt, BallStatus ball)
        {
            if (shootingFoulPending && !ball.LiveRelease && !ball.ShotInFlight)
            {
                ResolveShootingFoulMissed();
                return;
            }

            if (rules.useShotClock)
            {
                shotClock.Tick(dt, ball.Possessed);
                if (shotClock.Expired && ball.Possessed && possessionTeam.HasValue)
                {
                    Raise("SHOT CLOCK VIOLATION");
                    State.ResetForNextPossession();
                    Schedule(Pending.Possession, possessionTeam.Value.Opponent(), RestartKind.CheckBall);
                    return;
                }
            }

            if (rules.useGameClock && !suddenDeath)
            {
                gameClock.Tick(dt);
                // A shot (or pass) already in the air when time expires is played out.
                if (gameClock.Expired && !ball.ShotInFlight && !ball.LiveRelease) EndOfPeriod();
            }
        }

        private void EndOfPeriod()
        {
            State.ResetForNextPossession();
            bool regulationLeft = !gameClock.IsOvertime && gameClock.Period < rules.periods;
            if (regulationLeft)
            {
                int next = gameClock.Period + 1;
                gameClock.StartPeriod(next, rules.periodLengthSeconds, overtime: false);
                if (rules.teamFoulsResetEachPeriod) State.ResetTeamFouls();
                Raise($"End of period {next - 1}");
                TeamId offense = next % 2 == 1 ? rules.firstPossession : rules.firstPossession.Opponent();
                Schedule(Pending.Possession, offense, RestartKind.CheckBall);
                return;
            }

            if (State.ScoreHome != State.ScoreAway)
            {
                State.EndWith(State.ScoreHome > State.ScoreAway ? TeamId.Home : TeamId.Away);
                Raise("FINAL");
                return;
            }

            Raise("OVERTIME");
            if (rules.overtimeMode == OvertimeMode.SuddenDeathPoints)
            {
                suddenDeath = true;
                State.SetWinningScore(State.ScoreHome + rules.overtimePointsToWin);
            }
            else
            {
                gameClock.StartPeriod(gameClock.Period + 1, rules.overtimeLengthSeconds, overtime: true);
            }
            Schedule(Pending.Possession, rules.firstPossession.Opponent(), RestartKind.CheckBall);
        }

        // ---------- fouls and free throws ----------

        private void ResolveShootingFoulMissed()
        {
            shootingFoulPending = false;
            State.ResetForNextPossession();
            FoulPenalty penalty = FoulRules.Shooting(rules, State.GetTeamFouls(shootingFoul.FoulerTeam), shotScored: false, lastShotBeyondArc);
            AwardFreeThrowsOrBall(shootingFoul.FouledIndex, shootingFoul.FouledTeam, penalty);
        }

        private void AwardFreeThrowsOrBall(int fouledIndex, TeamId fouledTeam, FoulPenalty penalty)
        {
            if (penalty.FreeThrows > 0) StartFreeThrows(fouledIndex, fouledTeam, penalty);
            else Schedule(Pending.Possession, fouledTeam, RestartKind.CheckBall);
        }

        private void StartFreeThrows(int shooter, TeamId team, FoulPenalty penalty)
        {
            ftRemaining = penalty.FreeThrows;
            ftShooter = shooter;
            ftTeam = team;
            ftAfter = penalty.After;
            possessionTeam = team;
            needsClear = false;
            State.ResetForNextPossession();
            Schedule(Pending.FreeThrow, team, RestartKind.CheckBall);
        }

        private void TickFreeThrow(BallStatus ball)
        {
            if (!ftAwaitingResult) return;

            bool last = ftRemaining <= 1;
            if (last && ftAfter == AfterFreeThrows.LiveReboundOnMiss && !ball.ShotInFlight && ball.LiveRelease)
            {
                // Last free throw touched the rim: the ball is live, go rebound.
                ftAwaitingResult = false;
                ftRemaining = 0;
                shotClock.Reset(rules.shotClockSeconds);
                State.StartLivePlay();
                return;
            }
            if (!ball.LiveRelease) FinishFreeThrow(made: false);
        }

        private void HandleFreeThrowMade(TeamId team)
        {
            MatchPhase phase = State.Phase;
            if (phase != MatchPhase.FreeThrow && phase != MatchPhase.Live) return;

            State.AddPoints(team, rules.freeThrowPoints);
            Raise($"Free throw made ({team})");
            if (State.Phase == MatchPhase.Ended) return;

            if (phase == MatchPhase.Live)
            {
                // Last free throw rolled in after the rim made the ball live.
                State.ResetForNextPossession();
                Schedule(Pending.Possession, team.Opponent(), rules.afterMadeBasket);
                return;
            }
            FinishFreeThrow(made: true);
        }

        private void FinishFreeThrow(bool made)
        {
            ftAwaitingResult = false;
            ftRemaining--;
            State.ResetForNextPossession();
            if (!made) Raise("Free throw missed");

            if (ftRemaining > 0)
            {
                Schedule(Pending.FreeThrow, ftTeam, RestartKind.CheckBall);
                return;
            }
            if (ftAfter == AfterFreeThrows.FouledTeamPossession) Schedule(Pending.Possession, ftTeam, RestartKind.CheckBall);
            else if (made) Schedule(Pending.Possession, ftTeam.Opponent(), rules.afterMadeBasket);
            // Missed without touching the rim: dead ball, opponent's.
            else Schedule(Pending.Possession, ftTeam.Opponent(), RestartKind.CheckBall);
        }

        // ---------- restarts ----------

        private void Schedule(Pending what, TeamId team, RestartKind kind)
        {
            pending = what;
            pendingTeam = team;
            pendingKind = kind;
            pendingTimer = rules.restartDelaySeconds;
        }

        private void ExecutePending()
        {
            Pending what = pending;
            pending = Pending.None;
            if (what == Pending.Possession)
            {
                RestartPossession(pendingTeam, pendingKind);
            }
            else if (what == Pending.FreeThrow)
            {
                ftAwaitingResult = false;
                State.BeginFreeThrow();
                OnFreeThrowSetup?.Invoke(ftShooter);
            }
        }

        private void RestartPossession(TeamId offense, RestartKind kind)
        {
            State.ResetForNextPossession();
            possessionTeam = offense;
            needsClear = kind == RestartKind.UnderBasket && rules.clearBallOnChangeOfPossession;
            shotClock.Reset(rules.shotClockSeconds);
            shootingFoulPending = false;
            OnPossessionRestart?.Invoke(offense, kind);
            State.StartLivePlay();
            SyncState();
        }

        private void SyncState()
        {
            State.GameClock = rules.useGameClock ? (suddenDeath ? 0f : gameClock.Remaining) : -1f;
            State.Period = Mathf.Max(1, gameClock.Period);
            State.IsOvertime = suddenDeath || gameClock.IsOvertime;
            State.ShotClock = rules.useShotClock ? shotClock.Remaining : -1f;
            State.PossessionTeam = possessionTeam;
            State.BallMustBeCleared = needsClear;
        }

        private void Raise(string message) => OnRuleEvent?.Invoke(message);
    }
}
