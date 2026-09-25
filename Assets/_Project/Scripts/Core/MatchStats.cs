namespace Basket.Core
{
    public sealed class TeamStats
    {
        public int FieldGoalsAttempted;
        public int FieldGoalsMade;
        public int ThreesAttempted;
        public int ThreesMade;
        public int FreeThrowsAttempted;
        public int FreeThrowsMade;
        public int Passes;
        public int PassesCompleted;
        public int Steals;
        public int Blocks;
        public int Fouls;
        public int OffensiveRebounds;
        public int DefensiveRebounds;
        public int Turnovers;

        public override string ToString() =>
            $"FG {FieldGoalsMade}/{FieldGoalsAttempted}  3P {ThreesMade}/{ThreesAttempted}  FT {FreeThrowsMade}/{FreeThrowsAttempted}  " +
            $"PASS {PassesCompleted}/{Passes}  REB {OffensiveRebounds}+{DefensiveRebounds}  STL {Steals}  BLK {Blocks}  TO {Turnovers}  PF {Fouls}";
    }

    // Box score of one match, for the HUD, balancing simulations and (later) progression.
    public sealed class MatchStats
    {
        private readonly TeamStats home = new TeamStats();
        private readonly TeamStats away = new TeamStats();

        public TeamStats Get(TeamId team) => team == TeamId.Home ? home : away;
    }
}
