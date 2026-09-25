using UnityEngine;

namespace Basket.Core
{
    public interface IBallStateReadOnly
    {
        BallState CurrentState { get; }
        Vector3 Position { get; }
    }
}
