using UnityEngine;

namespace Basket.Core
{
    public readonly struct AIPerception
    {
        public readonly Vector3 SelfPosition;
        public readonly Vector3 OpponentPosition;
        public readonly Vector3 BallPosition;
        public readonly bool OpponentHasBall;
        public readonly bool SelfHasBall;

        public AIPerception(Vector3 selfPosition, Vector3 opponentPosition, Vector3 ballPosition, bool opponentHasBall, bool selfHasBall)
        {
            SelfPosition = selfPosition;
            OpponentPosition = opponentPosition;
            BallPosition = ballPosition;
            OpponentHasBall = opponentHasBall;
            SelfHasBall = selfHasBall;
        }
    }
}
