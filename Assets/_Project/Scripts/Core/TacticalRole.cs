namespace Basket.Core
{
    // What an AI player is doing for the team right now (for reading the game: HUD, debug,
    // future animation hooks). Derived from the ball and the team's orders:
    //   OffenseWithBall: has the ball (shoot / pass / drive decision).
    //   OffenseOffBall:  a teammate has it (spacing spot, cut, screen).
    //   DefenseOnBall:   guards the ball handler, between him and the rim.
    //   DefenseHelp:     off the ball on defense (deny, help side, zone).
    //   Idle:            loose ball / dead ball / no team plan.
    public enum TacticalRole { Idle, OffenseWithBall, OffenseOffBall, DefenseOnBall, DefenseHelp }
}
