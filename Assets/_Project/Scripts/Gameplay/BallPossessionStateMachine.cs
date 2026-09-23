using System;
using Basket.Core;

namespace Basket.Gameplay
{
    public sealed class BallPossessionStateMachine
    {
        public BallState CurrentState { get; private set; } = BallState.Free;
        public event Action<BallState, BallState> OnStateChanged;

        public bool TryTransition(BallState next)
        {
            if (!IsValidTransition(CurrentState, next)) return false;
            var prev = CurrentState;
            CurrentState = next;
            OnStateChanged?.Invoke(prev, next);
            return true;
        }

        private static bool IsValidTransition(BallState from, BallState to)
        {
            return (from, to) switch
            {
                (BallState.Free, BallState.Held) => true,
                (BallState.Held, BallState.Passing) => true,
                (BallState.Held, BallState.Shooting) => true,
                (BallState.Passing, BallState.Free) => true,
                (BallState.Shooting, BallState.Free) => true,
                _ => false
            };
        }
    }
}
