using UnityEngine;
using Basket.Core;

namespace Basket.UI
{
    public class DebugHud : MonoBehaviour
    {
        private IMatchState match;
        private IBallStateReadOnly ball;
        private IAIController ai;

        public void Configure(IMatchState matchState, IBallStateReadOnly ballState, IAIController aiController)
        {
            match = matchState;
            ball = ballState;
            ai = aiController;
        }

        private void OnGUI()
        {
            if (match == null || ball == null || ai == null) return;

            GUI.Label(new Rect(10, 10, 300, 20), $"Score: {match.ScoreHome} - {match.ScoreAway}  ({match.Phase})");
            GUI.Label(new Rect(10, 30, 300, 20), $"Ball: {ball.CurrentState}");
            GUI.Label(new Rect(10, 50, 300, 20), $"AI: {ai.CurrentState}");
        }
    }
}
