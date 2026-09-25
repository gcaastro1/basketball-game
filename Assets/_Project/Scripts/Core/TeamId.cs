namespace Basket.Core
{
    public enum TeamId { Home = 0, Away = 1 }

    public static class TeamIdExtensions
    {
        public const int TeamCount = 2;

        public static TeamId Opponent(this TeamId team) => team == TeamId.Home ? TeamId.Away : TeamId.Home;
    }
}
