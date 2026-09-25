using System;
using Basket.Core;

namespace Basket.Gameplay
{
    // Score, phase and rule state of one match, readable by UI/AI through IMatchState.
    // Only MatchManager changes the rule fields (internal setters).
    public sealed class MatchState : IMatchState
    {
        private readonly int[] scores = new int[TeamIdExtensions.TeamCount];
        private readonly int[] teamFouls = new int[TeamIdExtensions.TeamCount];

        // 0 = no score limit.
        public MatchState(int winningScore = 0)
        {
            WinningScore = winningScore;
        }

        public int WinningScore { get; private set; }
        public int ScoreHome => scores[(int)TeamId.Home];
        public int ScoreAway => scores[(int)TeamId.Away];
        public int GetScore(TeamId team) => scores[(int)team];
        public MatchPhase Phase { get; private set; } = MatchPhase.WaitingForInbound;
        public TeamId? Winner { get; private set; }

        public float GameClock { get; internal set; } = -1f;
        public int Period { get; internal set; } = 1;
        public bool IsOvertime { get; internal set; }
        public float ShotClock { get; internal set; } = -1f;
        public TeamId? PossessionTeam { get; internal set; }
        public bool BallMustBeCleared { get; internal set; }
        public int GetTeamFouls(TeamId team) => teamFouls[(int)team];

        public event Action OnScoreChanged;
        public event Action<MatchPhase, MatchPhase> OnPhaseChanged;

        public void StartLivePlay()
        {
            if (Phase == MatchPhase.Ended) return;
            SetPhase(MatchPhase.Live);
        }

        // A field goal during live play.
        public void RegisterScore(TeamId team, int points)
        {
            if (Phase != MatchPhase.Live) return;
            if (!AddPointsAndCheckWin(team, points)) SetPhase(MatchPhase.Scored);
        }

        // Points outside live play (free throws). Ends the match if the target is reached,
        // otherwise leaves the phase alone.
        public void AddPoints(TeamId team, int points)
        {
            if (Phase == MatchPhase.Ended) return;
            AddPointsAndCheckWin(team, points);
        }

        public void ResetForNextPossession()
        {
            if (Phase == MatchPhase.Ended) return;
            SetPhase(MatchPhase.WaitingForInbound);
        }

        internal void BeginFreeThrow()
        {
            if (Phase == MatchPhase.Ended) return;
            SetPhase(MatchPhase.FreeThrow);
        }

        internal void EndWith(TeamId winner)
        {
            if (Phase == MatchPhase.Ended) return;
            Winner = winner;
            SetPhase(MatchPhase.Ended);
        }

        internal void SetWinningScore(int score) => WinningScore = score;
        internal int AddTeamFoul(TeamId team) => ++teamFouls[(int)team];
        internal void ResetTeamFouls() => Array.Clear(teamFouls, 0, teamFouls.Length);

        private bool AddPointsAndCheckWin(TeamId team, int points)
        {
            scores[(int)team] += points;
            OnScoreChanged?.Invoke();
            if (WinningScore > 0 && scores[(int)team] >= WinningScore)
            {
                EndWith(team);
                return true;
            }
            return false;
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
