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
            SetState(next);
            return true;
        }

        // Rule-driven resets (check ball, inbound) bypass the normal transition table.
        public void ForceState(BallState next)
        {
            if (next == CurrentState) return;
            SetState(next);
        }

        private void SetState(BallState next)
        {
            var prev = CurrentState;
            CurrentState = next;
            OnStateChanged?.Invoke(prev, next);
        }

        // Passing/Shooting are real in-flight states: they last until the ball touches
        // something (-> Free) or, for a pass only, until someone catches it (-> Held).
        // A shot cannot be caught out of the air; it must touch rim/board/floor/player first.
        private static bool IsValidTransition(BallState from, BallState to)
        {
            return (from, to) switch
            {
                (BallState.Free, BallState.Held) => true,
                (BallState.Held, BallState.Passing) => true,
                (BallState.Held, BallState.Shooting) => true,
                (BallState.Passing, BallState.Held) => true,
                (BallState.Passing, BallState.Free) => true,
                (BallState.Shooting, BallState.Free) => true,
                _ => false
            };
        }
    }
}
