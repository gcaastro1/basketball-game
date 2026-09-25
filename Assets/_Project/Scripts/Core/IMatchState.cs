namespace Basket.Core
{
    public interface IMatchState
    {
        int ScoreHome { get; }
        int ScoreAway { get; }
        MatchPhase Phase { get; }
    }
}
