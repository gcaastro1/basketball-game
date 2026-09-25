using UnityEngine;
using Basket.Core;

namespace Basket.Gameplay
{
    public enum RestartKind { CheckBall, UnderBasket }
    public enum OvertimeMode { SuddenDeathPoints, TimedPeriod }

    // Scoring, clocks, possession and foul rules for one match mode. 3v3 and 5v5 are
    // different assets (see docs/decisoes.md, D-004 and D-011). Code defaults = the
    // generic 1v1 slice: 2/3 points, first to 21, no clocks.
    [CreateAssetMenu(fileName = "MatchRules", menuName = "Basket/Match Rules")]
    public class MatchRules : ScriptableObject
    {
        [Header("Scoring")]
        public int pointsInsideArc = 2;
        public int pointsBeyondArc = 3;
        public int freeThrowPoints = 1;
        public float threePointRadius = 6.75f;
        // 0 = no score limit (knockout win).
        public int winningScore = 21;

        [Header("Game clock (runs only while the ball is live)")]
        public bool useGameClock = false;
        public int periods = 1;
        public float periodLengthSeconds = 600f;

        [Header("Overtime (tied when time runs out)")]
        public OvertimeMode overtimeMode = OvertimeMode.SuddenDeathPoints;
        public int overtimePointsToWin = 2;
        public float overtimeLengthSeconds = 300f;

        [Header("Shot clock")]
        public bool useShotClock = false;
        public float shotClockSeconds = 12f;
        public float shotClockAfterRimTouch = 12f;

        [Header("Possession")]
        public TeamId firstPossession = TeamId.Home;
        public RestartKind afterMadeBasket = RestartKind.CheckBall;
        // 3x3: a live change of possession (defensive rebound, steal) must be cleared
        // beyond the arc before the team can score.
        public bool clearBallOnChangeOfPossession = false;
        public float restartDelaySeconds = 1.5f;

        [Header("Fouls (team fouls; 0 = never)")]
        public int teamFoulPenaltyThreshold = 7;
        public int teamFoulBonusPossessionThreshold = 10;
        public int penaltyFreeThrows = 2;
        public int shootingFoulFreeThrowsInsideArc = 2;
        public int shootingFoulFreeThrowsBeyondArc = 3;
        public int andOneFreeThrows = 1;
        public bool teamFoulsResetEachPeriod = true;
    }
}
