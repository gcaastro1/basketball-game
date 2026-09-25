using UnityEngine;

namespace Basket.AI
{
    public enum TeamOrderKind { None, Handle, Space, Cut, Screen, Roll, Crash, Guard, Help, BoxOut }

    public enum PlayType { Spacing, PickAndRoll, Isolation }

    // What the team wants one player to do right now.
    public readonly struct TeamOrder
    {
        public readonly TeamOrderKind Kind;
        public readonly Vector3 Target;
        // Player this order is about (man to guard, screener for the handler), or -1.
        public readonly int TargetIndex;

        public TeamOrder(TeamOrderKind kind, Vector3 target, int targetIndex = -1)
        {
            Kind = kind;
            Target = target;
            TargetIndex = targetIndex;
        }

        public static TeamOrder None => new TeamOrder(TeamOrderKind.None, Vector3.zero, -1);
    }
}
