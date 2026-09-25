using UnityEngine;
using Basket.Core;

namespace Basket.AI
{
    // Basic single-player AI behind IAIController (swappable for Utility AI / behavior
    // trees later). It plays through the same commands as a human: it holds and releases
    // the shoot button around its jump apex, jumps to block and to rebound, and reaches
    // for steals -- no stat or rule cheats.
    public sealed class AIAgentController : IAIController
    {
        private readonly AIConfig config;
        private readonly OpponentAIStateMachine fsm;
        private readonly System.Random rng;

        private float attackStartTime;
        private bool drivingThisPossession;
        private bool shotInProgress;
        private float releaseOffsetSeconds;
        private float shotStartTime;
        private float nextStealTime;

        public AIAgentController(AIConfig aiConfig = null, System.Random random = null)
        {
            config = aiConfig != null ? aiConfig : ScriptableObject.CreateInstance<AIConfig>();
            fsm = new OpponentAIStateMachine(config);
            rng = random ?? new System.Random();
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
            if (state == AIState.Attack && previous != AIState.Attack)
            {
                attackStartTime = p.Time;
                drivingThisPossession = rng.NextDouble() < config.driveChance;
            }
            if (!p.SelfHasBall) shotInProgress = false;

            return state switch
            {
                AIState.Attack => Attack(p),
                AIState.Chase => Chase(p),
                AIState.Guard => Defend(p, GuardSpot(p.OpponentPosition, p.DefendHoop, config.guardDistance), config.arrivalDistance),
                AIState.ContestShot => Defend(p, p.OpponentPosition, config.contestStandoff),
                _ => PlayerCommand.None
            };
        }

        private PlayerCommand Attack(AIPerception p)
        {
            float gravity = Mathf.Abs(Physics.gravity.y);
            // Shot never left the ground (e.g. possession was reset): start over.
            if (shotInProgress && p.SelfGrounded && p.Time - shotStartTime > 0.3f) shotInProgress = false;
            if (shotInProgress)
            {
                // Release when t >= apex + offset, i.e. vy <= -g * offset (vy = g * (tApex - t)).
                bool release = !p.SelfGrounded && p.SelfVelocity.y <= -gravity * releaseOffsetSeconds;
                if (release) shotInProgress = false;
                return new PlayerCommand(Vector2.zero, shootHeld: !release);
            }

            float range = drivingThisPossession ? config.driveFinishDistance : config.shootRange;
            bool inRange = FlatDistance(p.SelfPosition, p.AttackHoop) <= range;
            bool heldLongEnough = p.Time - attackStartTime >= config.minHoldSecondsBeforeShot;
            if (inRange && heldLongEnough && p.SelfGrounded)
            {
                shotInProgress = true;
                shotStartTime = p.Time;
                releaseOffsetSeconds = (float)(Gaussian() * config.releaseTimingJitterSeconds);
                // Sprinting into a close shot is what makes it a dunk attempt.
                return new PlayerCommand(Vector2.zero, sprint: drivingThisPossession, shootHeld: true);
            }
            return new PlayerCommand(MoveToward(p.SelfPosition, p.AttackHoop, range), sprint: drivingThisPossession);
        }

        private PlayerCommand Chase(AIPerception p)
        {
            Vector3 lead = p.BallPosition + new Vector3(p.BallVelocity.x, 0f, p.BallVelocity.z) * config.reboundLeadSeconds;
            float ballHeight = p.BallPosition.y - p.SelfPosition.y;
            bool jump = p.SelfGrounded && p.BallVelocity.y < 0f
                        && ballHeight >= config.reboundJumpMinHeight
                        && FlatDistance(p.SelfPosition, p.BallPosition) <= config.reboundJumpRange;
            return new PlayerCommand(MoveToward(p.SelfPosition, lead, config.arrivalDistance),
                sprint: config.sprintWhenChasing, jump: jump);
        }

        private PlayerCommand Defend(AIPerception p, Vector3 target, float arrival)
        {
            float toHandler = FlatDistance(p.SelfPosition, p.OpponentPosition);
            bool block = p.OpponentHasBall && p.SelfGrounded && !p.OpponentGrounded
                         && p.OpponentVelocity.y <= config.blockTriggerVerticalSpeed
                         && toHandler <= config.blockRange;

            bool steal = false;
            if (p.OpponentHasBall && p.OpponentGrounded && toHandler <= config.stealRange && p.Time >= nextStealTime)
            {
                steal = true;
                nextStealTime = p.Time + config.stealIntervalSeconds * (0.5f + (float)rng.NextDouble());
            }
            return new PlayerCommand(MoveToward(p.SelfPosition, target, arrival), jump: block, steal: steal);
        }

        // Standard normal sample (Box-Muller).
        private double Gaussian()
        {
            double u1 = 1.0 - rng.NextDouble();
            double u2 = rng.NextDouble();
            return System.Math.Sqrt(-2.0 * System.Math.Log(u1)) * System.Math.Cos(2.0 * System.Math.PI * u2);
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
