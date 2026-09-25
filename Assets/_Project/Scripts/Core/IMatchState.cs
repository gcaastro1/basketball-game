namespace Basket.Core
{
    public interface IMatchState
    {
        int ScoreHome { get; }
        int ScoreAway { get; }
        int GetScore(TeamId team);
        MatchPhase Phase { get; }
        TeamId? Winner { get; }
    }
}
