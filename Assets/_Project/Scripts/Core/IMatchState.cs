namespace Basket.Core
{
    public interface IMatchState
    {
        int ScoreHome { get; }
        int ScoreAway { get; }
        int GetScore(TeamId team);
        MatchPhase Phase { get; }
        TeamId? Winner { get; }

        // Clocks: negative when the rules do not use them.
        float GameClock { get; }
        int Period { get; }
        bool IsOvertime { get; }
        float ShotClock { get; }

        TeamId? PossessionTeam { get; }
        // 3x3: after a live change of possession the ball must be taken beyond the arc.
        bool BallMustBeCleared { get; }
        int GetTeamFouls(TeamId team);
    }
}
