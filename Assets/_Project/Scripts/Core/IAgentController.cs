namespace Basket.Core
{
    // Produces one player's command for the current tick. Called exactly once per
    // player per simulation tick, so implementations may keep per-tick state.
    public interface IAgentController
    {
        PlayerCommand Decide(MatchSnapshot snapshot, int selfIndex);
    }
}
