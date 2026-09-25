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
        private MatchStats stats;
        private bool showStats = true;
        private readonly List<string> events = new List<string>();

        public void Configure(IMatchState matchState, IBallStateReadOnly ballState, IReadOnlyList<IAIController> aiControllers,
            IMatchEventFeed eventFeed = null, MatchStats matchStats = null)
        {
            stats = matchStats;
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
            string clock = match.GameClock >= 0f ? $"{(match.IsOvertime ? "OT" : "P" + match.Period)} {Clock(match.GameClock)}" : "no game clock";
            string shotClock = match.ShotClock >= 0f ? $"   shot clock {match.ShotClock:0.0}" : "";
            Line(ref y, $"{clock}{shotClock}   fouls H{match.GetTeamFouls(TeamId.Home)} A{match.GetTeamFouls(TeamId.Away)}");
            if (match.BallMustBeCleared) Line(ref y, $"{match.PossessionTeam}: CLEAR THE BALL (take it beyond the arc)");
            if (match.Winner.HasValue) Line(ref y, $"{match.Winner.Value} wins!");
            Line(ref y, $"Ball: {ball.CurrentState}");
            if (ais != null)
            {
                for (int i = 0; i < ais.Count; i++) Line(ref y, $"AI {i}: {ais[i].CurrentState}");
            }
            Line(ref y, "WASD move | Shift sprint | Space: shoot (hold, release at top) / jump | E: pass / steal");
            if (stats != null && showStats)
            {
                y += 6f;
                Line(ref y, $"HOME  {stats.Get(TeamId.Home)}");
                Line(ref y, $"AWAY  {stats.Get(TeamId.Away)}");
            }
            y += 6f;
            foreach (string e in events) Line(ref y, e);
        }

        private static string Clock(float seconds)
        {
            int total = Mathf.CeilToInt(seconds);
            return $"{total / 60}:{total % 60:00}";
        }

        private static void Line(ref float y, string text)
        {
            GUI.Label(new Rect(10f, y, 900f, 20f), text);
            y += 20f;
        }
    }
}
