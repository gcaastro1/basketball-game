using UnityEngine;

namespace Basket.Core
{
    public interface IPlayerAgent
    {
        Vector2 GetMoveInput();
        bool WantsSprint();
        bool WantsDribbleAction();
        bool WantsPass();
        bool WantsShoot();
    }
}
