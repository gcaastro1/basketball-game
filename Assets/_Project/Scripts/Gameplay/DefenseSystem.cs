using System;
using System.Collections.Generic;
using UnityEngine;
using Basket.Core;

namespace Basket.Gameplay
{
    public enum StealOutcome { NotAttempted, Missed, Stolen }

    // Steals (on request, with a reach cooldown) and blocks (automatic: an airborne
    // defender whose raised arms meet a freshly released shot deflects it).
    public sealed class DefenseSystem
    {
        private readonly BallController ball;
        private readonly DefenseConfig config;
        private readonly System.Random rng;
        private readonly float[] nextStealTime;

        public event Action<int> OnSteal;
        public event Action<int> OnBlock;

        public DefenseSystem(int playerCount, BallController ball, DefenseConfig config, System.Random rng)
        {
            this.ball = ball;
            this.config = config;
            this.rng = rng ?? new System.Random();
            nextStealTime = new float[playerCount];
        }

        public StealOutcome TrySteal(int stealer, IReadOnlyList<PlayerEntity> players, bool handlerIsShooting, float time)
        {
            if (time < nextStealTime[stealer]) return StealOutcome.NotAttempted;
            if (ball.CurrentState != BallState.Held || handlerIsShooting) return StealOutcome.NotAttempted;

            PlayerEntity defender = players[stealer];
            if (!ball.CurrentHolder.TryGetComponent<PlayerEntity>(out var handler) || handler.Team == defender.Team) return StealOutcome.NotAttempted;

            Vector3 d = handler.FeetPosition - defender.FeetPosition;
            d.y = 0f;
            if (d.magnitude > config.stealRange) return StealOutcome.NotAttempted;
            if (!BlockMath.IsFacing(defender.transform.forward, defender.FeetPosition, handler.FeetPosition, config.stealFacingDot)) return StealOutcome.NotAttempted;

            nextStealTime[stealer] = time + config.stealCooldownSeconds;
            bool handlerMoving = handler.Motor.HorizontalVelocity.sqrMagnitude > 1f;
            float chance = config.stealBaseChance + (handlerMoving ? config.stealMovingHandlerBonus : 0f);
            if (rng.NextDouble() >= chance) return StealOutcome.Missed;

            Vector3 toDefender = -d.normalized;
            ball.KnockLoose(toDefender * config.stealKnockSpeed + Vector3.up * 1.5f, defender.Team);
            OnSteal?.Invoke(stealer);
            return StealOutcome.Stolen;
        }

        public void CheckBlocks(IReadOnlyList<PlayerEntity> players, TeamId? shootingTeam)
        {
            if (ball.CurrentState != BallState.Shooting || !shootingTeam.HasValue) return;
            if (Time.time - ball.LastReleaseTime > config.blockWindowSeconds) return;

            for (int i = 0; i < players.Count; i++)
            {
                PlayerEntity p = players[i];
                if (p.Team == shootingTeam.Value || p.Motor.IsGrounded) continue;
                if (!BlockMath.IsWithinArms(ball.Position, p.FeetPosition, config.headHeight, p.StandingReach, config.blockRadius)) continue;

                ball.KnockLoose(BlockMath.DeflectVelocity(ball.Position, p.FeetPosition, config.blockDeflectSpeed), p.Team);
                OnBlock?.Invoke(i);
                return;
            }
        }
    }
}
