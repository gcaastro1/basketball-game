using System;
using Basket.Core;

namespace Basket.Gameplay
{
    public sealed class MatchState : IMatchState
    {
        private readonly int[] scores = new int[TeamIdExtensions.TeamCount];

        // 0 = no score limit.
        public MatchState(int winningScore = 0)
        {
            WinningScore = winningScore;
        }

        public int WinningScore { get; }
        public int ScoreHome => scores[(int)TeamId.Home];
        public int ScoreAway => scores[(int)TeamId.Away];
        public int GetScore(TeamId team) => scores[(int)team];
        public MatchPhase Phase { get; private set; } = MatchPhase.WaitingForInbound;
        public TeamId? Winner { get; private set; }

        public event Action OnScoreChanged;
        public event Action<MatchPhase, MatchPhase> OnPhaseChanged;

        public void StartLivePlay()
        {
            if (Phase == MatchPhase.Ended) return;
            SetPhase(MatchPhase.Live);
        }

        public void RegisterScore(TeamId team, int points)
        {
            if (Phase != MatchPhase.Live) return;
            scores[(int)team] += points;
            OnScoreChanged?.Invoke();

            if (WinningScore > 0 && scores[(int)team] >= WinningScore)
            {
                Winner = team;
                SetPhase(MatchPhase.Ended);
            }
            else
            {
                SetPhase(MatchPhase.Scored);
            }
        }

        public void ResetForNextPossession()
        {
            if (Phase == MatchPhase.Ended) return;
            SetPhase(MatchPhase.WaitingForInbound);
        }

        private void SetPhase(MatchPhase next)
        {
            if (next == Phase) return;
            var prev = Phase;
            Phase = next;
            OnPhaseChanged?.Invoke(prev, next);
        }
    }
}
