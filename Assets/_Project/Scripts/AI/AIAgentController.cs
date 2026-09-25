using UnityEngine;
using Basket.Core;

namespace Basket.AI
{
    // Basic single-player AI: chase loose balls, guard/contest the handler, and with the
    // ball drive toward the rim and shoot once in range. A plain class behind
    // IAIController so it can later be swapped for Utility AI / behavior trees.
    public sealed class AIAgentController : IAIController
    {
        private readonly AIConfig config;
        private readonly OpponentAIStateMachine fsm;
        private float attackStartTime;

        public AIAgentController(AIConfig aiConfig = null)
        {
            config = aiConfig != null ? aiConfig : ScriptableObject.CreateInstance<AIConfig>();
            fsm = new OpponentAIStateMachine(config);
        }

        public AIState CurrentState => fsm.CurrentState;

        public PlayerCommand Decide(MatchSnapshot snapshot, int selfIndex)
        {
            return Decide(AIPerception.FromSnapshot(snapshot, selfIndex));
        }

        public PlayerCommand Decide(AIPerception p)
        {
            AIState previous = fsm.CurrentState;
            fsm.Evaluate(p);
            AIState state = fsm.CurrentState;
            if (state == AIState.Attack && previous != AIState.Attack) attackStartTime = p.Time;

            Vector3 target;
            float arrival = config.arrivalDistance;
            switch (state)
            {
                case AIState.Chase:
                    target = p.BallPosition;
                    break;
                case AIState.Guard:
                    target = GuardSpot(p.OpponentPosition, p.DefendHoop, config.guardDistance);
                    break;
                case AIState.ContestShot:
                    // Closing all the way to the handler's exact position makes the two
                    // bodies visually overlap -- found during manual playtesting.
                    target = p.OpponentPosition;
                    arrival = config.contestStandoff;
                    break;
                case AIState.Attack:
                    target = p.AttackHoop;
                    arrival = config.shootRange;
                    break;
                default:
                    target = p.SelfPosition;
                    break;
            }

            bool shoot = state == AIState.Attack
                         && FlatDistance(p.SelfPosition, p.AttackHoop) <= config.shootRange
                         && p.Time - attackStartTime >= config.minHoldSecondsBeforeShot;
            bool sprint = state == AIState.Chase && config.sprintWhenChasing;
            return new PlayerCommand(MoveToward(p.SelfPosition, target, arrival), sprint, pass: false, shoot: shoot);
        }

        // On the line from the man to the hoop, `distance` meters from the man.
        private static Vector3 GuardSpot(Vector3 man, Vector3 hoop, float distance)
        {
            Vector3 toHoop = hoop - man;
            toHoop.y = 0f;
            if (toHoop.sqrMagnitude < 0.0001f) return man;
            return man + toHoop.normalized * Mathf.Min(distance, toHoop.magnitude);
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            Vector3 d = b - a;
            d.y = 0f;
            return d.magnitude;
        }

        private static Vector2 MoveToward(Vector3 self, Vector3 target, float arrival)
        {
            Vector3 toTarget = target - self;
            toTarget.y = 0f;
            return toTarget.magnitude < arrival ? Vector2.zero : new Vector2(toTarget.x, toTarget.z).normalized;
        }
    }
}
