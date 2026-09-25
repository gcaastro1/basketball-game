using System.Collections.Generic;
using UnityEngine;
using Basket.Core;

namespace Basket.Gameplay
{
    public sealed class PassSystem
    {
        private readonly BallController ball;
        private readonly BallConfig config;
        private readonly Collider[] overlap = new Collider[16];
        private readonly List<Collider> nearby = new List<Collider>();

        public PassSystem(BallController ballController, BallConfig ballConfig)
        {
            ball = ballController;
            config = ballConfig;
        }

        public bool TryPass(Transform passer, Transform target)
        {
            if (ball.CurrentState != BallState.Held || ball.CurrentHolder != passer) return false;

            // A passer facing a wall holds the ball partly inside it (passes died there at
            // once -- "went loose off WallWest after 0.00 s"): release clear of the scenery.
            Vector3 origin = ball.ClearOfScenery(ball.HandPosition, passer.position);
            float apex = config.passApexHeight;
            if (passer.TryGetComponent<PlayerEntity>(out var passerEntity) && passerEntity.Tuning != null)
                apex *= passerEntity.AttributeMult(AttributeId.Passing, passerEntity.Tuning.passApex);
            Vector3 destination;
            if (target.TryGetComponent<PlayerEntity>(out var receiver))
            {
                destination = receiver.FeetPosition + Vector3.up * config.holdHeightAboveFeet;
                // Lead a moving receiver (a cutter): throw to where they will be. Found by
                // the AI-vs-AI simulation: passes aimed at a cutter's current position
                // landed behind them and became loose balls.
                float flight = TrajectoryMath.EstimateFlightTime(origin, destination, apex, Physics.gravity.y);
                Vector3 lead = Vector3.ClampMagnitude(receiver.Motor.HorizontalVelocity * flight, config.maxPassLead);
                destination += lead;
            }
            else
            {
                destination = target.position + Vector3.up * config.handHeightOffset;
            }
            Vector3 velocity = TrajectoryMath.ComputeCompensatedArcVelocity(origin, destination, apex,
                Physics.gravity.y, ball.LinearDamping, Time.fixedDeltaTime);

            ball.ReleaseAt(BallState.Passing, origin, velocity);
            ball.BeginPass(target, PlayersNear(origin));
            return true;
        }

        // Player bodies around the release point (the pass is thrown past them).
        private List<Collider> PlayersNear(Vector3 origin)
        {
            nearby.Clear();
            int count = Physics.OverlapSphereNonAlloc(origin, config.passProtectRadius, overlap,
                Physics.AllLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (overlap[i].TryGetComponent<PlayerEntity>(out var entity) && entity.BodyCollider == overlap[i])
                    nearby.Add(overlap[i]);
            }
            return nearby;
        }
    }
}
