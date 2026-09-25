using System.Collections.Generic;
using UnityEngine;
using Basket.Core;

namespace Basket.UI
{
    public class DebugHud : MonoBehaviour
    {
        private IMatchState match;
        private IBallStateReadOnly ball;
        private IReadOnlyList<IAIController> ais;

        public void Configure(IMatchState matchState, IBallStateReadOnly ballState, IReadOnlyList<IAIController> aiControllers)
        {
            match = matchState;
            ball = ballState;
            ais = aiControllers;
        }

        private void OnGUI()
        {
            if (match == null || ball == null) return;

            float y = 10f;
            Line(ref y, $"HOME {match.ScoreHome} - {match.ScoreAway} AWAY   ({match.Phase})");
            if (match.Winner.HasValue) Line(ref y, $"{match.Winner.Value} wins!");
            Line(ref y, $"Ball: {ball.CurrentState}");
            if (ais != null)
            {
                for (int i = 0; i < ais.Count; i++) Line(ref y, $"AI {i}: {ais[i].CurrentState}");
            }
            Line(ref y, "WASD move | Shift sprint | Space shoot | E pass");
        }

        private static void Line(ref float y, string text)
        {
            GUI.Label(new Rect(10f, y, 500f, 20f), text);
            y += 20f;
        }
    }
}
