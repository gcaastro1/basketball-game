namespace Basket.Core
{
    // Who decides a player's commands. Mode A (direct control of one athlete) is a roster
    // with one Human slot; the future Mode B (team/tactical control) becomes another
    // entry here plus a controller implementation -- the simulation does not change.
    public enum AgentControlType { Human, AI }
}
