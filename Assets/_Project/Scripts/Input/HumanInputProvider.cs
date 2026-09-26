using System;
using UnityEngine;
using Basket.Core;

namespace Basket.Input
{
    // Turns device input into PlayerCommands. Buttons are context-sensitive:
    //   with the ball:    Primary = shoot (hold, release near the apex), Secondary = pass
    //   without the ball: Primary = jump (block / rebound),              Secondary = steal
    public sealed class HumanInputProvider : IAgentController, IDisposable
    {
        public const float BufferSeconds = 0.15f;

        private readonly BasketInputActions actions = new BasketInputActions();
        private readonly InputBuffer primaryBuffer = new InputBuffer(BufferSeconds);
        private readonly InputBuffer secondaryBuffer = new InputBuffer(BufferSeconds);
        private bool? wasHolding;
        private Func<Vector3> viewForward;

        // Makes movement camera-relative: "up" goes where this direction points (on the floor).
        public void SetView(Func<Vector3> forward) => viewForward = forward;

        // A null snapshot means "no match context" (tests): treated as holding the ball.
        public PlayerCommand Decide(MatchSnapshot snapshot, int selfIndex)
        {
            float time = snapshot != null ? snapshot.Time : Time.time;
            bool holding = snapshot == null || snapshot.BallHolderIndex == selfIndex;

            // A press buffered for one role must not fire the other (steal -> catch -> pass).
            if (wasHolding.HasValue && wasHolding.Value != holding)
            {
                primaryBuffer.Clear();
                secondaryBuffer.Clear();
            }
            wasHolding = holding;

            if (actions.Primary.WasPressedThisFrame()) primaryBuffer.Press(time);
            if (actions.Secondary.WasPressedThisFrame()) secondaryBuffer.Press(time);

            Vector2 move = GetMoveInput();
            if (viewForward != null) move = CameraRelativeMove.Rotate(move, viewForward());
            bool sprint = actions.Sprint.IsPressed();
            bool ability = actions.Ability.WasPressedThisFrame();
            return holding
                ? new PlayerCommand(move, sprint, pass: secondaryBuffer.IsBuffered(time), shootHeld: actions.Primary.IsPressed(), ability: ability)
                : new PlayerCommand(move, sprint, jump: primaryBuffer.IsBuffered(time), steal: secondaryBuffer.IsBuffered(time), ability: ability);
        }

        public Vector2 GetMoveInput() => Vector2.ClampMagnitude(actions.Move.ReadValue<Vector2>(), 1f);

        public void Dispose() => actions.Dispose();
    }
}
