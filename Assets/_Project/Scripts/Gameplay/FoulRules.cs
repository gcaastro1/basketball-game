using Basket.Core;

namespace Basket.Gameplay
{
    public enum AfterFreeThrows
    {
        // Made last FT -> opponent's ball (like a made basket); missed -> live rebound.
        LiveReboundOnMiss,
        // The fouled team keeps the ball afterwards (check ball), made or missed.
        FouledTeamPossession
    }

    public readonly struct FoulEvent
    {
        public readonly TeamId FoulerTeam;
        public readonly int FouledIndex;
        public readonly TeamId FouledTeam;
        public readonly bool Shooting;

        public FoulEvent(TeamId foulerTeam, int fouledIndex, TeamId fouledTeam, bool shooting)
        {
            FoulerTeam = foulerTeam;
            FouledIndex = fouledIndex;
            FouledTeam = fouledTeam;
            Shooting = shooting;
        }
    }

    public readonly struct FoulPenalty
    {
        // 0 = no free throws (dead ball, fouled team inbounds).
        public readonly int FreeThrows;
        public readonly AfterFreeThrows After;

        public FoulPenalty(int freeThrows, AfterFreeThrows after)
        {
            FreeThrows = freeThrows;
            After = after;
        }
    }

    // What a foul is worth, from the rules and the fouling team's foul count (already
    // including this foul). Shooting fouls depend on the shot's outcome.
    public static class FoulRules
    {
        public static bool IsBonusPossession(MatchRules r, int teamFouls) =>
            r.teamFoulBonusPossessionThreshold > 0 && teamFouls >= r.teamFoulBonusPossessionThreshold;

        public static FoulPenalty NonShooting(MatchRules r, int teamFouls)
        {
            if (IsBonusPossession(r, teamFouls)) return new FoulPenalty(r.penaltyFreeThrows, AfterFreeThrows.FouledTeamPossession);
            if (r.teamFoulPenaltyThreshold > 0 && teamFouls >= r.teamFoulPenaltyThreshold)
                return new FoulPenalty(r.penaltyFreeThrows, AfterFreeThrows.LiveReboundOnMiss);
            return new FoulPenalty(0, AfterFreeThrows.FouledTeamPossession);
        }

        public static FoulPenalty Shooting(MatchRules r, int teamFouls, bool shotScored, bool beyondArc)
        {
            AfterFreeThrows after = IsBonusPossession(r, teamFouls) ? AfterFreeThrows.FouledTeamPossession : AfterFreeThrows.LiveReboundOnMiss;
            int count = shotScored ? r.andOneFreeThrows
                : beyondArc ? r.shootingFoulFreeThrowsBeyondArc : r.shootingFoulFreeThrowsInsideArc;
            return new FoulPenalty(count, after);
        }
    }
}
