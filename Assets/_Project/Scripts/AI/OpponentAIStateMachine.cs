using System;
using UnityEngine;
using Basket.Core;

namespace Basket.AI
{
    public sealed class OpponentAIStateMachine
    {
        public AIState CurrentState { get; private set; } = AIState.Idle;
        public event Action<AIState, AIState> OnStateChanged;

        public void Evaluate(AIPerception perception)
        {
            AIState next = DecideNextState(perception);
            if (next == CurrentState) return;

            var prev = CurrentState;
            CurrentState = next;
            OnStateChanged?.Invoke(prev, next);
        }

        private static AIState DecideNextState(AIPerception p)
        {
            if (p.OpponentHasBall)
            {
                float distToOpponent = Vector3.Distance(p.SelfPosition, p.OpponentPosition);
                return distToOpponent < 2.5f ? AIState.ContestShot : AIState.Guard;
            }
            if (p.SelfHasBall) return AIState.Idle;
            return AIState.Chase;
        }
    }
}
