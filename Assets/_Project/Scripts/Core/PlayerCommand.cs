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
        public readonly bool Shoot;

        public PlayerCommand(Vector2 move, bool sprint = false, bool pass = false, bool shoot = false)
        {
            Move = move;
            Sprint = sprint;
            Pass = pass;
            Shoot = shoot;
        }

        public static PlayerCommand None => default;
    }
}
