using UnityEngine;
using Basket.Core;

namespace Basket.AI
{
    // One AI player behind IAIController. It plays through the same commands as a human:
    // it holds and releases the shoot button around its jump apex, jumps to block and to
    // rebound, reaches for steals -- no stat or rule cheats.
    // With a TeamBrain it executes the team's orders (spacing, cuts, screens, help,
    // box-outs) and decides with the ball by utility; without one (1v1) it plays alone.
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
        private readonly TeamBrain brain;
        private MatchSnapshot snapshot;
        private int selfIndex = -1;
        // Where this possession's ball is being cleared to (kept so the handler doesn't dither).
        private Vector3? clearTarget;
        private TeamOrder order = TeamOrder.None;
        private readonly AITendencies tendencies;

        public AIAgentController(AIConfig aiConfig = null, System.Random random = null, TeamBrain teamBrain = null, AITendencies? characterTendencies = null)
        {
            tendencies = characterTendencies ?? AITendencies.Neutral;
            config = aiConfig != null ? aiConfig : ScriptableObject.CreateInstance<AIConfig>();
            fsm = new OpponentAIStateMachine(config);
            rng = random ?? new System.Random();
            brain = teamBrain;
        }

        public TeamOrder CurrentOrder => order;

        public AIState CurrentState => fsm.CurrentState;

        public PlayerCommand Decide(MatchSnapshot s, int self)
        {
            snapshot = s;
            selfIndex = self;
            // A pass is coming to me: go meet the ball instead of following team orders
            // (lead passes to a receiver who then changed direction landed alone and went
            // loose -- AI-vs-AI log).
            if (s.BallState == BallState.Passing && s.PassTargetIndex == self)
            {
                Vector3 me = s.GetPosition(self);
                Vector3 meet = PassArrival(s.BallPosition, s.BallVelocity, config.meetPassCatchHeight);
                snapshot = null;
                return new PlayerCommand(MoveToward(me, new Vector3(meet.x, me.y, meet.z), 0.2f), sprint: true);
            }
            int focus = -1;
            if (brain != null)
            {
                brain.Update(s);
                order = brain.GetOrder(self);
                focus = brain.GetMan(self);
            }
            PlayerCommand command = Decide(AIPerception.FromSnapshot(s, self, focus));
            snapshot = null;
            return command;
        }

        public PlayerCommand Decide(AIPerception p)
        {
            if (snapshot == null) order = TeamOrder.None;
            AIState previous = fsm.CurrentState;
            fsm.Evaluate(p);
            AIState state = fsm.CurrentState;
            if (state == AIState.Attack && previous != AIState.Attack)
            {
                attackStartTime = p.Time;
                drivingThisPossession = rng.NextDouble() < config.driveChance;
            }
            if (!p.SelfHasBall) shotInProgress = false;
            if (!p.SelfHasBall || !p.MustClear) clearTarget = null;

            return state switch
            {
                AIState.Attack => Attack(p),
                AIState.Chase => Chase(p),
                AIState.Guard => Guard(p),
                AIState.ContestShot => Defend(p, p.OpponentPosition, config.contestStandoff),
                AIState.Idle => FollowOrder(p),
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

            if (p.MustClear && snapshot != null)
            {
                // 3x3: take the ball beyond the arc, to the open side, before looking to score.
                clearTarget ??= TeamMath.ClearSpot(snapshot, selfIndex, p.ThreePointRadius + config.clearMargin,
                    config.clearSpotMaxAngle, config.clearSpotStepAngle, config.clearTravelWeight);
                return new PlayerCommand(Steer(p, clearTarget.Value, config.arrivalDistance, -1), sprint: true);
            }
            if (p.MustClear)
            {
                // 3x3: take the ball beyond the arc before looking to score.
                Vector3 outward = p.SelfPosition - p.AttackHoop;
                outward.y = 0f;
                if (outward.sqrMagnitude < 0.0001f) outward = Vector3.back;
                Vector3 clearSpot = new Vector3(p.AttackHoop.x, 0f, p.AttackHoop.z) + outward.normalized * (p.ThreePointRadius + config.clearMargin);
                return new PlayerCommand(MoveToward(p.SelfPosition, clearSpot, config.arrivalDistance), sprint: true);
            }

            bool freeThrow = p.Phase == MatchPhase.FreeThrow;
            bool urgent = p.ShotClock >= 0f && p.ShotClock <= config.shotClockUrgencySeconds;
            if (brain != null && snapshot != null && !freeThrow && !urgent) return TeamAttack(p);
            float range = drivingThisPossession ? config.driveFinishDistance : config.shootRange;
            bool inRange = freeThrow || urgent || FlatDistance(p.SelfPosition, p.AttackHoop) <= range;
            bool heldLongEnough = urgent || p.Time - attackStartTime >= config.minHoldSecondsBeforeShot;
            if (inRange && heldLongEnough && p.SelfGrounded)
            {
                bool finishing = drivingThisPossession && FlatDistance(p.SelfPosition, p.AttackHoop) <= config.driveFinishDistance;
                // At the buzzer there is no time to set the feet: a defender draped on the
                // handler can keep him moving until the clock runs out (AI-vs-AI shot clock
                // violations with no shot taken). Shooting on the move costs accuracy instead.
                if (!freeThrow && !finishing && !urgent && !FeetSet(p)) return PlayerCommand.None;
                shotInProgress = true;
                shotStartTime = p.Time;
                releaseOffsetSeconds = (float)(Gaussian() * ReleaseJitter());
                // Sprinting into a close shot is what makes it a dunk attempt.
                return new PlayerCommand(Vector2.zero, sprint: drivingThisPossession && !freeThrow, shootHeld: true);
            }
            return new PlayerCommand(MoveToward(p.SelfPosition, p.AttackHoop, range), sprint: drivingThisPossession);
        }

        // Team play with the ball: utility decision (shoot / pass / drive / hold).
        private PlayerCommand TeamAttack(AIPerception p)
        {
            HandlerAction action = BallHandlerDecision.Decide(snapshot, selfIndex, order, p.Time - attackStartTime, config, tendencies);
            switch (action.Kind)
            {
                case HandlerActionKind.Shoot:
                    if (!p.SelfGrounded) return PlayerCommand.None;
                    bool atRim = FlatDistance(p.SelfPosition, p.AttackHoop) <= config.driveFinishDistance;
                    if (!atRim && !FeetSet(p)) return PlayerCommand.None;
                    shotInProgress = true;
                    shotStartTime = p.Time;
                    releaseOffsetSeconds = (float)(Gaussian() * ReleaseJitter());
                    bool nearRim = FlatDistance(p.SelfPosition, p.AttackHoop) <= config.driveFinishDistance;
                    // Special abilities go with the shot they help (character tendency).
                    bool useAbility = snapshot.IsAbilityReady(selfIndex) && rng.NextDouble() < tendencies.abilityUsage;
                    return new PlayerCommand(Vector2.zero, sprint: nearRim, shootHeld: true, ability: useAbility);
                case HandlerActionKind.Pass:
                    Vector3 aim = action.MoveTarget - p.SelfPosition;
                    return new PlayerCommand(new Vector2(aim.x, aim.z).normalized, pass: true);
                case HandlerActionKind.Drive:
                    return new PlayerCommand(Steer(p, action.MoveTarget, config.arrivalDistance, -1), sprint: true);
                default:
                    return PlayerCommand.None;
            }
        }

        // Off-ball offense: spacing, cutting, screening, rolling, crashing the glass.
        private PlayerCommand FollowOrder(AIPerception p)
        {
            switch (order.Kind)
            {
                case TeamOrderKind.Space:
                    // Run the floor in transition, jog into spots in the half court.
                    return new PlayerCommand(Steer(p, order.Target, 0.4f, -1), sprint: FlatDistance(p.SelfPosition, order.Target) > config.sprintDistance);
                case TeamOrderKind.Cut:
                case TeamOrderKind.Roll:
                case TeamOrderKind.Crash:
                    return new PlayerCommand(Steer(p, order.Target, config.arrivalDistance, -1), sprint: true);
                case TeamOrderKind.Screen:
                    // No avoidance: the screener must get into the defender's path.
                    return new PlayerCommand(MoveToward(p.SelfPosition, order.Target, 0.15f));
                default:
                    return PlayerCommand.None;
            }
        }

        private PlayerCommand Guard(AIPerception p)
        {
            switch (order.Kind)
            {
                case TeamOrderKind.Help:
                    return Defend(p, order.Target, config.arrivalDistance, sprint: true);
                case TeamOrderKind.BoxOut:
                    // Seal the man: move to the spot between him and the rim, no avoidance.
                    return Defend(p, order.Target, 0.15f);
                case TeamOrderKind.Crash:
                    return new PlayerCommand(Steer(p, order.Target, config.arrivalDistance, -1), sprint: true);
                default:
                    // Get back on defense: sprint when far from where we need to be.
                    Vector3 spot = GuardSpot(p.OpponentPosition, p.DefendHoop, config.guardDistance);
                    return Defend(p, spot, config.arrivalDistance, sprint: FlatDistance(p.SelfPosition, spot) > config.sprintDistance);
            }
        }

        // Where a ball in flight comes down through `height` (ballistic, ignoring drag):
        // the receiver goes there rather than to where the ball is now (passes that passed
        // 1.1-1.2 m from a receiver chasing the ball's current position hit the floor).
        public static Vector3 PassArrival(Vector3 position, Vector3 velocity, float height)
        {
            float g = Mathf.Abs(Physics.gravity.y);
            float dy = position.y - height;
            // y(t) = y0 + vy t - g t^2 / 2 = height, descending root.
            float disc = velocity.y * velocity.y + 2f * g * dy;
            float t = disc > 0f ? (velocity.y + Mathf.Sqrt(disc)) / g : 0f;
            t = Mathf.Max(0f, t);
            return new Vector3(position.x + velocity.x * t, height, position.z + velocity.z * t);
        }

        // A jump shooter stops and sets their feet before rising (a jumper taken at full
        // speed carries the movement penalty: the AI-vs-AI log showed every open three at
        // 1.3x the standing error because it shot straight out of its run). Layups and
        // dunks keep their momentum.
        private bool FeetSet(AIPerception p) =>
            new Vector2(p.SelfVelocity.x, p.SelfVelocity.z).magnitude <= config.setFeetSpeed;

        // Offensive IQ tightens release timing around the apex.
        private float ReleaseJitter() =>
            config.releaseTimingJitterSeconds * (snapshot != null ? Attributes.Centered(snapshot.GetAttribute(selfIndex, AttributeId.OffensiveIQ), 1.8f, 0.4f) : 1f);

        // Move toward target, steering around other players when playing as a team.
        private Vector2 Steer(AIPerception p, Vector3 target, float arrival, int ignore)
        {
            Vector2 dir = MoveToward(p.SelfPosition, target, arrival);
            if (snapshot == null || dir == Vector2.zero) return dir;
            return TeamMath.Avoid(snapshot, selfIndex, dir, ignore, config.avoidanceRadius);
        }

        private PlayerCommand Chase(AIPerception p)
        {
            float anticipation = snapshot != null ? Attributes.Centered(snapshot.GetAttribute(selfIndex, AttributeId.ReboundPositioning), 0.4f, 1.6f) : 1f;
            Vector3 lead = p.BallPosition + new Vector3(p.BallVelocity.x, 0f, p.BallVelocity.z) * config.reboundLeadSeconds * anticipation;
            float ballHeight = p.BallPosition.y - p.SelfPosition.y;
            bool jump = p.SelfGrounded && p.BallVelocity.y < 0f
                        && ballHeight >= config.reboundJumpMinHeight
                        && FlatDistance(p.SelfPosition, p.BallPosition) <= config.reboundJumpRange;
            return new PlayerCommand(MoveToward(p.SelfPosition, lead, config.arrivalDistance),
                sprint: config.sprintWhenChasing, jump: jump);
        }

        private PlayerCommand Defend(AIPerception p, Vector3 target, float arrival, bool sprint = false)
        {
            float toHandler = FlatDistance(p.SelfPosition, p.OpponentPosition);
            bool onBall = p.OpponentHasBall && p.FocusHasBall;
            // Defensive IQ: better defenders wait for the shooter's apex instead of jumping early.
            float trigger = config.blockTriggerVerticalSpeed
                            * (snapshot != null ? Attributes.Centered(snapshot.GetAttribute(selfIndex, AttributeId.DefensiveIQ), 1.8f, 0.5f) : 1f);
            bool block = onBall && p.SelfGrounded && !p.OpponentGrounded
                         && p.OpponentVelocity.y <= trigger
                         && toHandler <= config.blockRange;

            bool steal = false;
            if (onBall && p.OpponentGrounded && toHandler <= config.stealRange && p.Time >= nextStealTime)
            {
                steal = true;
                float aggression = tendencies.stealAggression > 0.01f ? tendencies.stealAggression : 1f;
                nextStealTime = p.Time + config.stealIntervalSeconds / aggression * (0.5f + (float)rng.NextDouble());
            }
            return new PlayerCommand(MoveToward(p.SelfPosition, target, arrival), sprint: sprint, jump: block, steal: steal);
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
