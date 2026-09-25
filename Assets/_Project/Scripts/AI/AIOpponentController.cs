using UnityEngine;
using Basket.Core;

namespace Basket.AI
{
    public class AIOpponentController : MonoBehaviour, IAIController
    {
        // The AI shoots immediately on gaining possession regardless of distance from
        // the rim (no positioning/dribble-to-basket logic yet -- see the brief for
        // this task). Without a cooldown, a shot taken right under the backboard has a
        // short-enough flight that the AI's own Chase state re-catches its own
        // rebound and re-shoots the very next possible frame, producing a rapid
        // catch-shoot loop that looks broken (found during manual playtesting, once
        // ball pickup itself started working reliably). This cooldown doesn't fix the
        // underlying "no positioning" simplification -- it just keeps the loop from
        // being instantaneous.
        //
        // First attempt used 1.5s and it did NOT actually help: a close-range shot's
        // own flight time (apex-height-driven, not distance-driven -- see
        // TrajectoryMath) is itself around 1.7-2s, so the cooldown had always already
        // expired by the time the ball came back down and got re-caught. It never
        // once blocked a real re-shoot. 4s is comfortably longer than that natural
        // cycle, so it actually enforces a pause.
        private const float ShotCooldownSeconds = 4f;

        private readonly OpponentAIStateMachine fsm = new();
        private Vector2 currentMoveInput;
        private bool selfHasBall;
        private float lastShotAttemptTime = float.NegativeInfinity;

        public AIState CurrentState => fsm.CurrentState;

        public void Tick(AIPerception perception)
        {
            fsm.Evaluate(perception);
            selfHasBall = perception.SelfHasBall;
            currentMoveInput = ComputeMoveInput(fsm.CurrentState, perception);
        }

        // ContestShot's target is the opponent's exact position. Closing all the way
        // to it makes the two CharacterControllers visually overlap/clip into each
        // other -- found during manual playtesting. Every other state's target is
        // either already a fair distance away in normal play (Chase/Guard) or is the
        // AI's own position (Idle), so only ContestShot needs a wider stop distance
        // than the default "arrived" threshold.
        private const float DefaultArrivalDistance = 0.2f;
        private const float ContestStandoffDistance = 1.3f;

        private static Vector2 ComputeMoveInput(AIState state, AIPerception p)
        {
            Vector3 targetPos = state switch
            {
                AIState.Chase => p.BallPosition,
                AIState.Guard => Vector3.Lerp(p.OpponentPosition, p.SelfPosition, 0.3f),
                AIState.ContestShot => p.OpponentPosition,
                _ => p.SelfPosition
            };
            float arrivalDistance = state == AIState.ContestShot ? ContestStandoffDistance : DefaultArrivalDistance;

            Vector3 toTarget = targetPos - p.SelfPosition;
            toTarget.y = 0f;
            return toTarget.magnitude < arrivalDistance ? Vector2.zero : new Vector2(toTarget.x, toTarget.z).normalized;
        }

        public Vector2 GetMoveInput() => currentMoveInput;
        public bool WantsSprint() => fsm.CurrentState == AIState.Chase;
        public bool WantsDribbleAction() => false;
        public bool WantsPass() => false;

        public bool WantsShoot()
        {
            if (!selfHasBall) return false;
            if (Time.time - lastShotAttemptTime < ShotCooldownSeconds) return false;

            lastShotAttemptTime = Time.time;
            return true;
        }
    }
}
