using UnityEngine;
using Basket.Core;

namespace Basket.Gameplay
{
    public class MatchManager : MonoBehaviour
    {
        private readonly MatchState matchState = new();
        private BallController ball;

        public MatchState State => matchState;

        public void Configure(BallController ballController)
        {
            ball = ballController;
            ball.OnScored += OnBallScored;
            matchState.StartLivePlay();
        }

        private void OnBallScored(Transform scorer)
        {
            // Vertical slice: single hoop, always credit "home" — real home/away attribution
            // belongs to the future 3v3/5v5 subproject once there are two hoops.
            matchState.RegisterScore(homeScored: true, points: 2);
            matchState.ResetForNextPossession();
            matchState.StartLivePlay();
        }

        private void OnDestroy()
        {
            if (ball != null) ball.OnScored -= OnBallScored;
        }
    }
}
