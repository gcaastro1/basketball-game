using UnityEngine;

namespace Basket.Core
{
    // Everything a controller (human input, AI, scripted test) asks one player to do
    // for one simulation tick. Commands are plain data so the simulation never needs to
    // know who produced them, and so they can later be recorded, replayed or sent over
    // a network without touching gameplay code.
    public readonly struct PlayerCommand
    {
        public readonly Vector2 Move;
        public readonly bool Sprint;
        public readonly bool Pass;
        // Level, not edge: going true starts a shot (the player jumps), going false
        // releases it. Release timing relative to the jump apex affects accuracy.
        public readonly bool ShootHeld;
        public readonly bool Jump;
        public readonly bool Steal;
        // Activate the character's active ability.
        public readonly bool Ability;
        // Level: defensive stance while held (without the ball) -- slower, facing the ball.
        public readonly bool Guard;

        public PlayerCommand(Vector2 move, bool sprint = false, bool pass = false, bool shootHeld = false, bool jump = false, bool steal = false, bool ability = false, bool guard = false)
        {
            Guard = guard;
            Ability = ability;
            Move = move;
            Sprint = sprint;
            Pass = pass;
            ShootHeld = shootHeld;
            Jump = jump;
            Steal = steal;
        }

        public static PlayerCommand None => default;
    }
}
