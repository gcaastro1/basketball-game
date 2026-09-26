using System;
using UnityEngine;
using Basket.Core;
using Basket.Characters;

namespace Basket.Gameplay
{
    // Shooting, layups and dunks for every player.
    // Press shoot (while holding the ball on the ground) -> the player jumps with the ball
    // raised; a jump shot releases when the button is let go (best at the jump apex);
    // layups and dunks finish automatically at the apex. Landing with the ball forces a
    // (very late) release.
    public sealed class ShotSystem : IShotReportSource
    {
        private enum Phase { None, Rising }

        // Ignore "landed" for a moment after takeoff (the controller may still report ground).
        private const float MinAirTime = 0.1f;

        private readonly BallController ball;
        private readonly ShotConfig config;
        private readonly BallConfig ballConfig;
        private readonly System.Random rng;
        private readonly AttributeTuning tuning;
        private readonly float threePointRadius;

        private readonly Phase[] phase;
        private readonly ShotType[] type;
        private readonly float[] startTime;
        private readonly float[] apexTime;
        private readonly float[] takeoffSpeedRatio;
        private readonly bool[] previousHeld;
        private int freeThrowShooter = -1;

        public event Action<ShotReport> OnShotTaken;

        // Each shot goes at the basket the shooter's team attacks (from the snapshot).
        public ShotSystem(int playerCount, BallController ball, ShotConfig config, BallConfig ballConfig, System.Random rng,
            AttributeTuning tuning = null, float threePointRadius = 6.75f)
        {
            this.tuning = tuning != null ? tuning : ScriptableObject.CreateInstance<AttributeTuning>();
            this.threePointRadius = threePointRadius;
            this.ball = ball;
            this.config = config;
            this.ballConfig = ballConfig;
            this.rng = rng ?? new System.Random();
            phase = new Phase[playerCount];
            type = new ShotType[playerCount];
            startTime = new float[playerCount];
            apexTime = new float[playerCount];
            takeoffSpeedRatio = new float[playerCount];
            previousHeld = new bool[playerCount];
        }

        public bool IsShooting(int index) => phase[index] != Phase.None;

        // For presentation: the shot in progress and how far it is from takeoff (0) to the
        // jump apex (1). False when the player is not shooting.
        public bool TryGetShot(int index, float time, out ShotType shotType, out float progress)
        {
            shotType = type[index];
            progress = 0f;
            if (phase[index] == Phase.None) return false;
            float span = apexTime[index] - startTime[index];
            progress = span > 1e-4f ? Mathf.Clamp01((time - startTime[index]) / span) : 1f;
            return true;
        }

        // During a free throw only this player shoots, and their shot is a free throw.
        public void SetFreeThrowShooter(int index) => freeThrowShooter = index;

        public void ResetAll()
        {
            for (int i = 0; i < phase.Length; i++) phase[i] = Phase.None;
        }

        public void Tick(int index, PlayerEntity player, PlayerCommand command, MatchSnapshot snapshot, float time)
        {
            bool pressed = command.ShootHeld && !previousHeld[index];
            previousHeld[index] = command.ShootHeld;
            bool holding = ball.CurrentState == BallState.Held && ball.CurrentHolder == player.transform;

            if (phase[index] == Phase.None)
            {
                if (holding && pressed && player.Motor.IsGrounded) Begin(index, player, command, snapshot, time);
                return;
            }
            if (!holding)
            {
                phase[index] = Phase.None;
                return;
            }

            ball.SetHeldLocalOffset(Vector3.up * (config.shotPocketHeight - ballConfig.holdHeightAboveFeet));

            bool landed = player.Motor.IsGrounded && time - startTime[index] > MinAirTime;
            bool atApex = player.Motor.Velocity.y <= 0f;
            switch (type[index])
            {
                case ShotType.JumpShot:
                case ShotType.FreeThrow:
                    if (!command.ShootHeld || landed) Release(index, player, snapshot, time);
                    break;
                case ShotType.Layup:
                    if (atApex || landed) Release(index, player, snapshot, time);
                    break;
                case ShotType.Dunk:
                    if (atApex || landed) FinishDunk(index, player, snapshot, time);
                    break;
            }
        }

        private static Vector3 RimFor(MatchSnapshot s, PlayerEntity p) => s.GetAttackingHoop(p.Team);

        private void Begin(int index, PlayerEntity player, PlayerCommand command, MatchSnapshot snapshot, float time)
        {
            Vector3 rimCenter = RimFor(snapshot, player);
            PlayerMotor motor = player.Motor;
            Vector3 feet = player.FeetPosition;
            float distance = FlatDistance(feet, rimCenter);
            float reachAtApex = feet.y + player.StandingReach + motor.JumpHeight;
            ShotType shotType = index == freeThrowShooter
                ? ShotType.FreeThrow
                : ShotAccuracyModel.Classify(distance, command.Sprint, reachAtApex, rimCenter.y, config);
            // Dunking takes the Dunk attribute as well as the reach.
            if (shotType == ShotType.Dunk && Attr(player, AttributeId.Dunk) < tuning.minDunkAttribute) shotType = ShotType.Layup;

            float timeToApex = motor.JumpSpeed / Mathf.Abs(Physics.gravity.y);
            takeoffSpeedRatio[index] = motor.HorizontalVelocity.magnitude / Mathf.Max(0.01f, motor.MaxSpeed);

            bool jumped;
            if (shotType == ShotType.Dunk)
            {
                // Lunge so the raised hand arrives near the rim at the apex.
                Vector3 toRim = new Vector3(rimCenter.x - feet.x, 0f, rimCenter.z - feet.z);
                float lungeDistance = Mathf.Max(0f, toRim.magnitude - config.dunkFinishReach * 0.3f);
                Vector3 lunge = toRim.sqrMagnitude > 0.0001f
                    ? toRim.normalized * Mathf.Min(config.maxDunkLungeSpeed, lungeDistance / timeToApex)
                    : Vector3.zero;
                jumped = motor.Jump(lunge);
            }
            else if (shotType == ShotType.JumpShot || shotType == ShotType.FreeThrow)
            {
                // A jump shot goes (mostly) straight up; a layup keeps the drive's momentum.
                jumped = motor.Jump(motor.HorizontalVelocity * 0.3f);
            }
            else
            {
                jumped = motor.Jump();
            }
            if (!jumped) return;

            phase[index] = Phase.Rising;
            type[index] = shotType;
            startTime[index] = time;
            apexTime[index] = time + timeToApex;
        }

        private void Release(int index, PlayerEntity player, MatchSnapshot snapshot, float time)
        {
            Vector3 rimCenter = RimFor(snapshot, player);
            ShotType shotType = type[index] == ShotType.Dunk ? ShotType.Layup : type[index];
            Vector3 feet = player.FeetPosition;
            float distance = FlatDistance(feet, rimCenter);
            float timingError = time - apexTime[index];
            float contest = shotType == ShotType.FreeThrow ? 0f : MaxContest(index, feet, snapshot);
            if (distance <= tuning.closeShotMaxDistance && shotType != ShotType.FreeThrow)
            {
                // Strong / skilled post players finish through contact.
                float contact = Mathf.Min(player.AttributeMult(AttributeId.Strength, tuning.contactContest),
                                          player.AttributeMult(AttributeId.PostScoring, tuning.contactContest));
                contest = Mathf.Clamp01(contest * contact);
            }

            float rating = player.Attributes != null
                ? Attributes.Normalized(player.Attributes.Get(tuning.ShotAttribute(shotType, distance, threePointRadius)))
                : config.defaultShooterRating;
            var input = new ShotAccuracyInput(shotType, distance, timingError, contest, takeoffSpeedRatio[index], rating);
            float errorRadius = ShotAccuracyModel.ErrorRadius(input, config);
            errorRadius *= Mathf.Lerp(1f, tuning.tiredErrorAtEmpty, tuning.Tiredness(player.Stamina));
            ShotEffect effect = player.Abilities != null ? player.Abilities.TakeShotEffect(shotType) : ShotEffect.None;
            errorRadius *= effect.errorMultiplier;

            Vector3 target = rimCenter + ShotMath.SampleDiscOffset(errorRadius, rng);
            float arc = (shotType == ShotType.Layup ? config.layupArcHeight : ShotArc.ApexAboveRim(distance, config)) * effect.arcHeightMultiplier;
            // A shooter against the placeholder arena's walls (corner and wing spots sit
            // right at them) had the ball overhead partly inside the wall: those shots died
            // there at once (shot traces: 4 of 11 long AI jumpers "first touched WallEast at
            // 0.01 s"). Release clear of the scenery and aim from there.
            Vector3 origin = ball.ClearOfScenery(ball.Position, player.transform.position);
            Vector3 velocity = TrajectoryMath.ComputeCompensatedArcVelocity(origin, target, arc,
                Physics.gravity.y, ball.LinearDamping, Time.fixedDeltaTime);

            phase[index] = Phase.None;
            ball.ReleaseAt(BallState.Shooting, origin, velocity, shotType);
            ball.BeginShotTrace(rimCenter, target);
            OnShotTaken?.Invoke(new ShotReport(index, shotType, distance, timingError, contest, errorRadius));
        }

        private void FinishDunk(int index, PlayerEntity player, MatchSnapshot snapshot, float time)
        {
            Vector3 rimCenter = RimFor(snapshot, player);
            Vector3 hand = player.ReachPoint;
            bool canFinish = FlatDistance(hand, rimCenter) <= config.dunkFinishReach
                             && hand.y >= rimCenter.y + config.dunkReachClearance;
            if (!canFinish)
            {
                // Could not get to the rim: turns into a layup attempt.
                Release(index, player, snapshot, time);
                return;
            }

            phase[index] = Phase.None;
            Vector3 feet = player.FeetPosition;
            float contest = MaxContest(index, feet, snapshot);
            if (player.Abilities != null) player.Abilities.TakeShotEffect(ShotType.Dunk);
            ball.ReleaseAt(BallState.Shooting, rimCenter + Vector3.up * (ball.Radius + 0.1f), Vector3.down * config.dunkBallDropSpeed, ShotType.Dunk);
            OnShotTaken?.Invoke(new ShotReport(index, ShotType.Dunk, FlatDistance(feet, rimCenter), 0f, contest, 0f));
        }

        private float MaxContest(int shooter, Vector3 shooterFeet, MatchSnapshot s)
        {
            Vector3 rimCenter = s.GetAttackingHoop(s.GetTeam(shooter));
            TeamId team = s.GetTeam(shooter);
            float max = 0f;
            for (int i = 0; i < s.PlayerCount; i++)
            {
                if (s.GetTeam(i) == team) continue;
                float c = ContestMath.Contest(shooterFeet, rimCenter, s.GetPosition(i), !s.IsGrounded(i),
                    config.contestRadius, config.airborneContestBonus);
                if (c <= 0f) continue;
                // Better defenders contest harder: Interior near the rim, Perimeter away from it.
                AttributeId skill = FlatDistance(shooterFeet, rimCenter) <= tuning.closeShotMaxDistance
                    ? AttributeId.InteriorDefense : AttributeId.PerimeterDefense;
                c *= AttributeTuning.Mult(s.GetAttributes(i), skill, tuning.defenderContest);
                max = Mathf.Max(max, Mathf.Clamp01(c));
            }
            return max;
        }

        private static float Attr(PlayerEntity p, AttributeId id) => p.Attributes != null ? p.Attributes.Get(id) : Attributes.Neutral;

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
