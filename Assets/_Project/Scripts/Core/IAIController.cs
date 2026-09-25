namespace Basket.Core
{
    public interface IAIController : IPlayerAgent
    {
        AIState CurrentState { get; }
        void Tick(AIPerception perception);
    }
}
