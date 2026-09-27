namespace Basket.Core
{
    public interface IAIController : IAgentController
    {
        AIState CurrentState { get; }
        TacticalRole CurrentRole { get; }
    }
}
