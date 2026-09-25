using System.Collections.Generic;
using UnityEngine;
using Basket.Core;

namespace Basket.UI
{
    public class DebugHud : MonoBehaviour
    {
        private const int MaxEvents = 5;

        private IMatchState match;
        private IBallStateReadOnly ball;
        private IReadOnlyList<IAIController> ais;
        private IMatchEventFeed feed;
        private readonly List<string> events = new List<string>();

        public void Configure(IMatchState matchState, IBallStateReadOnly ballState, IReadOnlyList<IAIController> aiControllers, IMatchEventFeed eventFeed = null)
        {
            match = matchState;
            ball = ballState;
            ais = aiControllers;
            if (feed != null) feed.OnMatchEvent -= AddEvent;
            feed = eventFeed;
            if (feed != null) feed.OnMatchEvent += AddEvent;
        }

        private void AddEvent(string message)
        {
            events.Insert(0, message);
            if (events.Count > MaxEvents) events.RemoveAt(events.Count - 1);
        }

        private void OnDestroy()
        {
            if (feed != null) feed.OnMatchEvent -= AddEvent;
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
            Line(ref y, "WASD move | Shift sprint | Space: shoot (hold, release at top) / jump | E: pass / steal");
            y += 6f;
            foreach (string e in events) Line(ref y, e);
        }

        private static void Line(ref float y, string text)
        {
            GUI.Label(new Rect(10f, y, 900f, 20f), text);
            y += 20f;
        }
    }
}
