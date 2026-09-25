using System;
using UnityEngine;
using Basket.Core;

namespace Basket.Gameplay
{
    // Match rules and flow: turns made baskets into points for the right team, ends the
    // match, and decides who gets the ball next. Knows nothing about physics or player
    // placement -- it asks for a possession restart and MatchSimulation carries it out.
    public sealed class MatchManager
    {
        private readonly MatchRules rules;
        private readonly Vector3 rimCenter;
        private bool restartPending;
        private float restartTimer;
        private TeamId nextOffense;

        public MatchState State { get; }
        public event Action<TeamId> OnPossessionRestart;

        public MatchManager(MatchRules matchRules, Vector3 rimCenter)
        {
            rules = matchRules;
            this.rimCenter = rimCenter;
            State = new MatchState(matchRules.winningScore);
        }

        public void BeginMatch() => RestartPossession(rules.firstPossession);

        public void HandleScore(ScoreEvent scoreEvent)
        {
            if (State.Phase != MatchPhase.Live || scoreEvent.Team == null) return;

            TeamId team = scoreEvent.Team.Value;
            int points = ScoringMath.PointsForRelease(scoreEvent.ReleasePosition, rimCenter,
                rules.threePointRadius, rules.pointsInsideArc, rules.pointsBeyondArc);
            State.RegisterScore(team, points);
            if (State.Phase == MatchPhase.Ended) return;

            // Make-it-take-it is a later rules option; for now possession alternates.
            nextOffense = team.Opponent();
            restartTimer = rules.restartDelaySeconds;
            restartPending = true;
        }

        // Ball left the play area (e.g. over the placeholder walls). Provisional rule: the
        // other team from whoever touched it last gets a check ball. Real out-of-bounds
        // lines/inbounds are Etapa 3.
        public void HandleBallOutOfPlay(TeamId? lastTouchTeam)
        {
            if (State.Phase != MatchPhase.Live) return;
            State.ResetForNextPossession();
            nextOffense = lastTouchTeam.HasValue ? lastTouchTeam.Value.Opponent() : rules.firstPossession;
            restartTimer = rules.restartDelaySeconds;
            restartPending = true;
        }

        public void Tick(float dt)
        {
            if (!restartPending) return;
            restartTimer -= dt;
            if (restartTimer > 0f) return;
            restartPending = false;
            RestartPossession(nextOffense);
        }

        private void RestartPossession(TeamId offense)
        {
            State.ResetForNextPossession();
            OnPossessionRestart?.Invoke(offense);
            State.StartLivePlay();
        }
    }
}
