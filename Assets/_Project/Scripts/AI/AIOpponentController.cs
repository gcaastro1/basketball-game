using UnityEngine;
using Basket.Core;

namespace Basket.AI
{
    public class AIOpponentController : MonoBehaviour, IAIController
    {
        private readonly OpponentAIStateMachine fsm = new();
        private Vector2 currentMoveInput;
        private bool selfHasBall;

        public AIState CurrentState => fsm.CurrentState;

        public void Tick(AIPerception perception)
        {
            fsm.Evaluate(perception);
            selfHasBall = perception.SelfHasBall;
            currentMoveInput = ComputeMoveInput(fsm.CurrentState, perception);
        }

        private static Vector2 ComputeMoveInput(AIState state, AIPerception p)
        {
            Vector3 targetPos = state switch
            {
                AIState.Chase => p.BallPosition,
                AIState.Guard => Vector3.Lerp(p.OpponentPosition, p.SelfPosition, 0.3f),
                AIState.ContestShot => p.OpponentPosition,
                _ => p.SelfPosition
            };
            Vector3 toTarget = targetPos - p.SelfPosition;
            toTarget.y = 0f;
            return toTarget.sqrMagnitude < 0.04f ? Vector2.zero : new Vector2(toTarget.x, toTarget.z).normalized;
        }

        public Vector2 GetMoveInput() => currentMoveInput;
        public bool WantsSprint() => fsm.CurrentState == AIState.Chase;
        public bool WantsDribbleAction() => false;
        public bool WantsPass() => false;
        public bool WantsShoot() => selfHasBall;
    }
}
