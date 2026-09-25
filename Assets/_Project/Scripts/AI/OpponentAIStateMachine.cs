using System;
using UnityEngine;
using Basket.Core;

namespace Basket.AI
{
    public sealed class OpponentAIStateMachine
    {
        private readonly AIConfig config;

        public OpponentAIStateMachine(AIConfig aiConfig = null)
        {
            config = aiConfig != null ? aiConfig : ScriptableObject.CreateInstance<AIConfig>();
        }

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

        private AIState DecideNextState(AIPerception p)
        {
            if (p.SelfHasBall) return AIState.Attack;
            if (p.OpponentHasBall)
            {
                // Off-ball defender: stay with the assigned man, between him and the hoop.
                if (!p.FocusHasBall) return AIState.Guard;
                float distToOpponent = Vector3.Distance(p.SelfPosition, p.OpponentPosition);
                return distToOpponent < config.contestDistance ? AIState.ContestShot : AIState.Guard;
            }
            // Off-ball offense (spacing, cuts, screens) belongs to the teammate AI stage.
            if (p.TeammateHasBall) return AIState.Idle;
            // Loose ball: only the closest player of each team goes for it; the rest
            // stay with their man.
            return p.ClosestToBall ? AIState.Chase : AIState.Guard;
        }
    }
}
