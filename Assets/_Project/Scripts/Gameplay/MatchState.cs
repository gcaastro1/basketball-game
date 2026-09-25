using System;
using Basket.Core;

namespace Basket.Gameplay
{
    public sealed class MatchState : IMatchState
    {
        public int ScoreHome { get; private set; }
        public int ScoreAway { get; private set; }
        public MatchPhase Phase { get; private set; } = MatchPhase.WaitingForInbound;

        public event Action OnScoreChanged;
        public event Action<MatchPhase, MatchPhase> OnPhaseChanged;

        public void StartLivePlay() => SetPhase(MatchPhase.Live);

        public void RegisterScore(bool homeScored, int points)
        {
            if (Phase != MatchPhase.Live) return;
            if (homeScored) ScoreHome += points; else ScoreAway += points;
            OnScoreChanged?.Invoke();
            SetPhase(MatchPhase.Scored);
        }

        public void ResetForNextPossession() => SetPhase(MatchPhase.WaitingForInbound);

        private void SetPhase(MatchPhase next)
        {
            if (next == Phase) return;
            var prev = Phase;
            Phase = next;
            OnPhaseChanged?.Invoke(prev, next);
        }

        internal void StartLivePlayForTest() => StartLivePlay();
    }
}
