using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Basket.Core;
using Basket.AI;

public class TeamAITests
{
    private static readonly Vector3 Rim = new Vector3(0f, 3.05f, 13.1f);
    private static readonly Vector3 RimFloor = new Vector3(0f, 0f, 13.1f);
    private AIConfig config;

    [SetUp]
    public void SetUp()
    {
        config = ScriptableObject.CreateInstance<AIConfig>();
        config.teamReplanInterval = 0f;
    }

    // Home 0..2 attack, Away 3..5 defend, man-to-man i <-> i+3.
    private static MatchSnapshot ThreeOnThree(Vector3[] positions, int holder, float time = 1f)
    {
        var s = new MatchSnapshot(6);
        for (int i = 0; i < 6; i++) s.SetPlayer(i, i < 3 ? TeamId.Home : TeamId.Away, positions[i], Vector3.zero);
        for (int i = 0; i < 3; i++)
        {
            s.SetMatchup(i, i + 3);
            s.SetMatchup(i + 3, i);
        }
        s.SetAttackingHoop(TeamId.Home, Rim);
        s.SetAttackingHoop(TeamId.Away, Rim);
        s.SetCourtCenter(new Vector3(0f, 0f, 7f));
        s.SetBall(holder >= 0 ? positions[holder] + Vector3.up : Vector3.zero, holder >= 0 ? BallState.Held : BallState.Free, holder);
        s.SetMatch(MatchPhase.Live, time);
        return s;
    }

    private static Vector3[] Standard() => new[]
    {
        new Vector3(0f, 0f, 5.5f),  // 0 handler, top
        new Vector3(-5f, 0f, 8f),   // 1 left wing
        new Vector3(5f, 0f, 8f),    // 2 right wing
        new Vector3(0f, 0f, 6.5f),  // 3 on handler
        new Vector3(-4.5f, 0f, 9f), // 4 on 1
        new Vector3(4.5f, 0f, 9f),  // 5 on 2
    };

    // ---------- floor reads ----------

    [Test]
    public void ShotValue_OpenCloseShot_BeatsContestedLongShot()
    {
        var pos = Standard();
        pos[1] = new Vector3(0.5f, 0f, 11.5f);  // open, near the rim
        pos[4] = new Vector3(-6f, 0f, 3f);
        var s = ThreeOnThree(pos, holder: 0);
        Assert.Greater(TeamMath.ShotValue(s, 1, config), TeamMath.ShotValue(s, 0, config));
    }

    [Test]
    public void PassSafety_DefenderInTheLane_IsLow()
    {
        var pos = Standard();
        pos[4] = new Vector3(-2.5f, 0f, 6.75f); // on the line 0 -> 1
        var s = ThreeOnThree(pos, holder: 0);
        Assert.Less(TeamMath.PassSafety(s, 0, 1, config), 0.3f);
        Assert.Greater(TeamMath.PassSafety(s, 0, 2, config), 0.9f);
    }

    [Test]
    public void DriveLane_BlockedByDefenderInFront()
    {
        var s = ThreeOnThree(Standard(), holder: 0);
        Assert.IsFalse(TeamMath.DriveLaneOpen(s, 0, 1.2f));
        var pos = Standard();
        pos[3] = new Vector3(3f, 0f, 5f);
        pos[4] = new Vector3(-6f, 0f, 5f);
        pos[5] = new Vector3(6f, 0f, 5f);
        Assert.IsTrue(TeamMath.DriveLaneOpen(ThreeOnThree(pos, holder: 0), 0, 1.2f));
    }

    [Test]
    public void Avoid_BendsAroundAPlayerInTheWay()
    {
        var pos = Standard();
        pos[3] = new Vector3(0.1f, 0f, 6.3f); // right in front of player 0 heading +z
        var s = ThreeOnThree(pos, holder: 0);
        Vector2 steered = TeamMath.Avoid(s, 0, Vector2.up, -1, 1.1f);
        Assert.Greater(Mathf.Abs(steered.x), 0.3f);
        Assert.Greater(steered.y, 0f, "still heading forward");
    }

    // ---------- planners ----------

    [Test]
    public void SpacingSlots_AreOnThePerimeterAndDistinct()
    {
        List<Vector3> slots = Basket.AI.OffensePlanner.SpacingSlots(RimFloor, Vector3.back, 7.3f, 2, clearOut: false);
        Assert.AreEqual(2, slots.Count);
        foreach (var slot in slots) Assert.AreEqual(7.3f, TeamMath.FlatDistance(slot, RimFloor), 0.01f);
        Assert.Greater(Vector3.Distance(slots[0], slots[1]), 5f);
    }

    [Test]
    public void AssignSlots_GivesEachPlayerTheNearestFreeSlot()
    {
        var players = new List<Vector3> { new Vector3(5f, 0f, 8f), new Vector3(-5f, 0f, 8f) };
        var slots = new List<Vector3> { new Vector3(-6f, 0f, 9f), new Vector3(6f, 0f, 9f) };
        int[] a = Basket.AI.OffensePlanner.AssignSlots(players, slots);
        Assert.AreEqual(1, a[0]);
        Assert.AreEqual(0, a[1]);
    }

    [Test]
    public void ScreenSpot_IsBesideTheDefender()
    {
        Vector3 spot = Basket.AI.OffensePlanner.ScreenSpot(new Vector3(0f, 0f, 5.5f), new Vector3(0f, 0f, 6.5f), RimFloor, 1, 0.8f);
        Assert.AreEqual(0.8f, Vector3.Distance(spot, new Vector3(0f, 0f, 6.5f)), 0.01f);
        Assert.AreEqual(6.5f, spot.z, 0.01f);
    }

    [Test]
    public void Beaten_OnlyWhenNearTheRimWithTheDefenderTrailing()
    {
        Assert.IsTrue(DefensePlanner.IsBeaten(new Vector3(0f, 0f, 11f), new Vector3(0f, 0f, 9f), RimFloor, 3.5f, 0.4f));
        Assert.IsFalse(DefensePlanner.IsBeaten(new Vector3(0f, 0f, 11f), new Vector3(0f, 0f, 12f), RimFloor, 3.5f, 0.4f), "defender between him and the rim");
        Assert.IsFalse(DefensePlanner.IsBeaten(new Vector3(0f, 0f, 6f), new Vector3(0f, 0f, 4f), RimFloor, 3.5f, 0.4f), "too far to need help");
    }

    // ---------- team brain ----------

    [Test]
    public void Offense_OffBallPlayersSpaceTheFloor()
    {
        config.spacingPlayWeight = 1f;
        config.pickAndRollPlayWeight = 0f;
        config.isolationPlayWeight = 0f;
        config.cutTriggerDistance = 99f; // no cuts in this test
        var brain = new TeamBrain(TeamId.Home, 6, config, rng: new System.Random(1));
        brain.Update(ThreeOnThree(Standard(), holder: 0));

        Assert.AreEqual(TeamOrderKind.Handle, brain.GetOrder(0).Kind);
        Assert.AreEqual(TeamOrderKind.Space, brain.GetOrder(1).Kind);
        Assert.AreEqual(TeamOrderKind.Space, brain.GetOrder(2).Kind);
        Assert.Less(brain.GetOrder(1).Target.x, 0f, "left wing keeps the left side");
        Assert.Greater(brain.GetOrder(2).Target.x, 0f);
    }

    [Test]
    public void Offense_PlayerWithSaggingDefender_Cuts()
    {
        config.spacingPlayWeight = 1f;
        config.pickAndRollPlayWeight = 0f;
        config.isolationPlayWeight = 0f;
        var pos = Standard();
        pos[4] = new Vector3(-1f, 0f, 11f); // left wing's defender sags off toward the rim... far from his man
        var brain = new TeamBrain(TeamId.Home, 6, config, rng: new System.Random(1));
        brain.Update(ThreeOnThree(pos, holder: 0));

        Assert.AreEqual(TeamOrderKind.Cut, brain.GetOrder(1).Kind);
        Assert.AreEqual(TeamOrderKind.Space, brain.GetOrder(2).Kind, "one cutter at a time");
    }

    [Test]
    public void Offense_PickAndRoll_ClosestTeammateScreensTheHandlersDefender()
    {
        config.spacingPlayWeight = 0f;
        config.pickAndRollPlayWeight = 1f;
        config.isolationPlayWeight = 0f;
        var pos = Standard();
        pos[1] = new Vector3(-2f, 0f, 6.5f); // closest to the handler
        var brain = new TeamBrain(TeamId.Home, 6, config, rng: new System.Random(1));
        brain.Update(ThreeOnThree(pos, holder: 0));

        Assert.AreEqual(PlayType.PickAndRoll, brain.CurrentPlay);
        TeamOrder screen = brain.GetOrder(1);
        Assert.AreEqual(TeamOrderKind.Screen, screen.Kind);
        Assert.Less(Vector3.Distance(screen.Target, pos[3]), 1.0f, "next to the handler's defender");
    }

    [Test]
    public void Defense_BeatenDriver_BringsHelpFromTheNearestTeammate()
    {
        var pos = Standard();
        pos[0] = new Vector3(0.5f, 0f, 11f);  // driver near the rim
        pos[3] = new Vector3(0f, 0f, 8.5f);   // his defender trailing
        pos[4] = new Vector3(-2f, 0f, 11f);   // nearest helper
        var brain = new TeamBrain(TeamId.Away, 6, config, rng: new System.Random(1));
        brain.Update(ThreeOnThree(pos, holder: 0));

        Assert.AreEqual(TeamOrderKind.Help, brain.GetOrder(4).Kind);
        Assert.AreEqual(TeamOrderKind.Position, brain.GetOrder(5).Kind, "off the ball: deny / help-side spot");
    }

    [Test]
    public void Defense_ScreenOnTheHandlersDefender_Switches()
    {
        var pos = Standard();
        pos[3] = new Vector3(0f, 0f, 8f);     // handler's defender, 2.5 m off the handler...
        pos[1] = new Vector3(0.5f, 0f, 8.3f); // ...with an attacker (screener) on him
        pos[4] = new Vector3(-1f, 0f, 9f);    // screener's defender
        var brain = new TeamBrain(TeamId.Away, 6, config, rng: new System.Random(1));
        brain.Update(ThreeOnThree(pos, holder: 0));

        Assert.AreEqual(0, brain.GetMan(4), "screener's defender takes the handler");
        Assert.AreEqual(1, brain.GetMan(3), "handler's defender takes the screener");
    }

    [Test]
    public void Rebound_DefendersBoxOut_ShootingTeamCrashesOne()
    {
        var pos = Standard();
        var defense = new TeamBrain(TeamId.Away, 6, config, rng: new System.Random(1));
        var offense = new TeamBrain(TeamId.Home, 6, config, rng: new System.Random(1));
        var s = ThreeOnThree(pos, holder: 0);
        defense.Update(s);
        offense.Update(s);

        var shot = ThreeOnThree(pos, holder: -1, time: 2f);
        shot.SetBall(new Vector3(0f, 5f, 9f), BallState.Shooting, -1);
        defense.Update(shot);
        offense.Update(shot);

        for (int d = 3; d < 6; d++) Assert.AreEqual(TeamOrderKind.BoxOut, defense.GetOrder(d).Kind);
        int crashers = 0;
        for (int o = 0; o < 3; o++) if (offense.GetOrder(o).Kind == TeamOrderKind.Crash) crashers++;
        Assert.AreEqual(1, crashers);
        Assert.AreNotEqual(TeamOrderKind.Crash, offense.GetOrder(0).Kind, "the shooter does not crash");
    }

    // ---------- ball handler utility ----------

    [Test]
    public void Handler_OpenGoodShot_Shoots()
    {
        var pos = Standard();
        pos[0] = new Vector3(0f, 0f, 8.5f);   // 4.6 m, open
        pos[3] = new Vector3(3f, 0f, 3f);
        var s = ThreeOnThree(pos, holder: 0);
        Assert.AreEqual(HandlerActionKind.Shoot, BallHandlerDecision.Decide(s, 0, TeamOrder.None, 1f, config).Kind);
    }

    [Test]
    public void Handler_ContestedWithAnOpenTeammate_Passes()
    {
        var pos = Standard();
        pos[0] = new Vector3(0f, 0f, 5.5f);
        pos[3] = new Vector3(0f, 0f, 6.3f);   // right on him
        pos[2] = new Vector3(3f, 0f, 10.5f);  // open near the rim
        pos[5] = new Vector3(6.5f, 0f, 3f);   // his defender far away
        var s = ThreeOnThree(pos, holder: 0);
        HandlerAction a = BallHandlerDecision.Decide(s, 0, TeamOrder.None, 1f, config);
        Assert.AreEqual(HandlerActionKind.Pass, a.Kind);
        Assert.AreEqual(2, a.PassTarget);
    }

    [Test]
    public void Handler_ScreenReady_AttacksOffTheScreen()
    {
        var s = ThreeOnThree(Standard(), holder: 0);
        var order = new TeamOrder(TeamOrderKind.Handle, new Vector3(2f, 0f, 8f), targetIndex: 1);
        HandlerAction a = BallHandlerDecision.Decide(s, 0, order, 0.2f, config);
        Assert.AreEqual(HandlerActionKind.Drive, a.Kind);
        Assert.AreEqual(new Vector3(2f, 0f, 8f), a.MoveTarget);
    }

    [Test]
    public void Handler_NearTheRim_Finishes()
    {
        var pos = Standard();
        pos[0] = new Vector3(0f, 0f, 11.6f);
        Assert.AreEqual(HandlerActionKind.Shoot, BallHandlerDecision.Decide(ThreeOnThree(pos, holder: 0), 0, TeamOrder.None, 0f, config).Kind);
    }

    [Test]
    public void Handler_NeverHoldsForever()
    {
        var s = ThreeOnThree(Standard(), holder: 0);
        HandlerAction a = BallHandlerDecision.Decide(s, 0, TeamOrder.None, config.forceDecisionSeconds + 0.1f, config);
        Assert.AreNotEqual(HandlerActionKind.Hold, a.Kind);
    }

    [Test]
    public void Handler_HeldTooLong_WithADefenderDraped_AttacksInsteadOfForcingTheJumper()
    {
        var pos = Standard();
        // Holder beyond the arc with their defender a step away; teammates smothered too.
        for (int i = 3; i < 6; i++) pos[i] = pos[i - 3] + new Vector3(0f, 0f, 0.6f);
        var s = ThreeOnThree(pos, holder: 0);
        Assume.That(TeamMath.ContestRead(s, 0, config), Is.GreaterThan(config.forcedShotMaxContest));

        HandlerAction a = BallHandlerDecision.Decide(s, 0, TeamOrder.None, config.forceDecisionSeconds + 0.1f, config);
        Assert.AreNotEqual(HandlerActionKind.Shoot, a.Kind);
        Assert.AreNotEqual(HandlerActionKind.Hold, a.Kind);
    }

    [Test]
    public void Receiver_OfAPassInFlight_GoesToMeetTheBall()
    {
        var s = ThreeOnThree(Standard(), holder: -1);
        // Ball in flight from the handler (top) toward the left wing (player 1 at -5, 8).
        var ballPos = new Vector3(-2f, 1.5f, 6.5f);
        s.SetBall(ballPos, BallState.Passing, -1, new Vector3(-6f, 0f, 3f));
        s.SetPassTarget(1);
        var brain = new TeamBrain(TeamId.Home, 6, config, rng: new System.Random(1));
        var ai = new AIAgentController(config, new System.Random(1), brain);

        PlayerCommand cmd = ai.Decide(s, 1);
        Vector3 toBall = ballPos - s.GetPosition(1);
        Assert.Greater(Vector2.Dot(cmd.Move.normalized, new Vector2(toBall.x, toBall.z).normalized), 0.8f, "moves toward the ball");
        Assert.IsTrue(cmd.Sprint);
    }

    [Test]
    public void PassArrival_IsWhereTheBallComesDownToCatchHeight()
    {
        Vector3 at = AIAgentController.PassArrival(new Vector3(0f, 2f, 0f), new Vector3(4f, 0f, 0f), 1f);
        float t = Mathf.Sqrt(2f / Mathf.Abs(Physics.gravity.y));
        Assert.AreEqual(4f * t, at.x, 1e-3f);
        Assert.AreEqual(1f, at.y, 1e-5f);

        // Rising ball: the descending crossing, not the rising one.
        Vector3 lob = AIAgentController.PassArrival(new Vector3(0f, 1.5f, 0f), new Vector3(0f, 3f, 5f), 1.5f);
        Assert.AreEqual(5f * 2f * 3f / Mathf.Abs(Physics.gravity.y), lob.z, 1e-3f);
    }

    [Test]
    public void AutoStrategy_FollowsWeights()
    {
        config.spacingPlayWeight = 0f;
        config.pickAndRollPlayWeight = 0f;
        config.isolationPlayWeight = 1f;
        var strategy = new AutoStrategy(config, new System.Random(1));
        Assert.AreEqual(PlayType.Isolation, strategy.ChoosePlay(ThreeOnThree(Standard(), 0), TeamId.Home, 0));
    }

    [Test]
    public void Controller_WithBrain_SpacesWhenATeammateHasTheBall()
    {
        config.spacingPlayWeight = 1f;
        config.pickAndRollPlayWeight = 0f;
        config.isolationPlayWeight = 0f;
        config.cutTriggerDistance = 99f;
        var brain = new TeamBrain(TeamId.Home, 6, config, rng: new System.Random(1));
        var ai = new AIAgentController(config, new System.Random(1), brain);
        var pos = Standard();
        pos[1] = new Vector3(-2f, 0f, 7f); // off his spot

        var cmd = ai.Decide(ThreeOnThree(pos, holder: 0), 1);
        Assert.AreEqual(TeamOrderKind.Space, ai.CurrentOrder.Kind);
        Assert.Greater(cmd.Move.sqrMagnitude, 0.5f, "moves to his spacing spot");
    }

    // ---------- positioning (help defense, zone, spacing) ----------

    [Test]
    public void OffBallSpot_OnePassAway_Denies_WeakSide_SagsToHelp()
    {
        Vector3 ball = new Vector3(-6f, 0f, 7f);
        // Man one pass away from the ball: tight, between him and the rim, a step toward the ball.
        Vector3 nearMan = new Vector3(-6f, 0f, 11.5f);
        Vector3 deny = DefenseFormation.OffBallSpot(nearMan, ball, RimFloor, config);
        Assert.Less(TeamMath.FlatDistance(deny, nearMan), 2.2f, "stays with a man one pass away");
        Assert.Less(TeamMath.FlatDistance(deny, RimFloor), TeamMath.FlatDistance(nearMan, RimFloor), "rim side of him");

        // Man on the weak side, far from the ball: well off him, toward the lane on the ball side.
        Vector3 farMan = new Vector3(6.5f, 0f, 10f);
        Vector3 help = DefenseFormation.OffBallSpot(farMan, ball, RimFloor, config);
        Assert.Greater(TeamMath.FlatDistance(help, farMan), 2.5f, "sags off a man two passes away");
        Assert.LessOrEqual(TeamMath.FlatDistance(help, farMan), config.maxSagFromMan + 1e-3f, "can still close out");
        Assert.Less(Mathf.Abs(help.x), Mathf.Abs(farMan.x) - 2f, "toward the middle (help side)");
    }

    [Test]
    public void ManDefense_WeakSideDefenderHelps_InsteadOfHuggingHisMan()
    {
        var pos = Standard();
        pos[0] = new Vector3(-6f, 0f, 7f);    // ball on the left
        pos[3] = new Vector3(-5.5f, 0f, 8f);  // on the ball
        pos[2] = new Vector3(6.5f, 0f, 10f);  // weak-side attacker
        pos[5] = new Vector3(6f, 0f, 10.5f);  // his defender, hugging him
        var brain = new TeamBrain(TeamId.Away, 6, config, rng: new System.Random(1));
        brain.Update(ThreeOnThree(pos, holder: 0));

        Assert.AreEqual(DefenseScheme.ManToMan, brain.CurrentDefense);
        Assert.AreEqual(TeamOrderKind.Guard, brain.GetOrder(3).Kind, "on the ball");
        TeamOrder weak = brain.GetOrder(5);
        Assert.AreEqual(TeamOrderKind.Position, weak.Kind);
        Assert.Greater(TeamMath.FlatDistance(weak.Target, pos[2]), 2.5f, "weak side sags toward the lane");
    }

    [Test]
    public void Zone_OneDefenderOnTheBall_OthersHoldOrMarkTheirZones()
    {
        config.zoneDefenseChance = 1f;
        var pos = Standard();
        var brain = new TeamBrain(TeamId.Away, 6, config, rng: new System.Random(1));
        brain.Update(ThreeOnThree(pos, holder: 0));

        Assert.AreEqual(DefenseScheme.Zone, brain.CurrentDefense);
        int onBall = 0, zone = 0;
        for (int d = 3; d < 6; d++)
        {
            TeamOrder o = brain.GetOrder(d);
            if (o.Kind == TeamOrderKind.Guard && o.TargetIndex == 0) onBall++;
            if (o.Kind == TeamOrderKind.Zone)
            {
                zone++;
                Assert.AreNotEqual(0, o.TargetIndex, "zone defenders do not double the ball");
                Assert.Less(TeamMath.FlatDistance(o.Target, RimFloor), 7f, "inside the arc, around the rim");
            }
        }
        Assert.AreEqual(1, onBall, "exactly one defender on the ball");
        Assert.AreEqual(2, zone);
    }

    [Test]
    public void ZoneSpots_SlideTowardTheBall_AndStayInFrontOfTheRim()
    {
        Vector3 axis = new Vector3(0f, 0f, -1f); // court center is toward -z
        var left = DefenseFormation.ZoneSpots(RimFloor, axis, new Vector3(-6f, 0f, 7f), 5, config);
        var right = DefenseFormation.ZoneSpots(RimFloor, axis, new Vector3(6f, 0f, 7f), 5, config);
        Assert.AreEqual(5, left.Count);
        for (int i = 0; i < 5; i++)
        {
            Assert.Less(left[i].x, right[i].x, "slides with the ball");
            Assert.LessOrEqual(left[i].z, RimFloor.z - 0.8f + 1e-3f, "never behind the rim");
        }
    }

    [Test]
    public void Spacing_OffBallSpotsKeepAwayFromTheHandler()
    {
        config.spacingPlayWeight = 1f;
        config.pickAndRollPlayWeight = 0f;
        config.isolationPlayWeight = 0f;
        config.cutTriggerDistance = 99f;
        var pos = Standard();
        pos[0] = new Vector3(-5f, 0f, 8.5f); // handler on the left wing
        pos[1] = new Vector3(-4f, 0f, 7f);   // teammate right next to him
        var brain = new TeamBrain(TeamId.Home, 6, config, rng: new System.Random(1));
        brain.Update(ThreeOnThree(pos, holder: 0));

        for (int i = 1; i < 3; i++)
        {
            Assert.AreEqual(TeamOrderKind.Space, brain.GetOrder(i).Kind);
            Assert.GreaterOrEqual(TeamMath.FlatDistance(brain.GetOrder(i).Target, pos[0]), config.spacingMinFromHandler - 1e-3f);
        }
    }

    [Test]
    public void CornerThree_IsAThreeForTheAI_AndCornerSpotsStayInside()
    {
        var pos = Standard();
        pos[1] = new Vector3(-6.9f, 0f, 13.1f); // corner, 6.9 m to the side: inside 7.24 but past 6.71
        pos[4] = new Vector3(-1f, 0f, 3f);
        var s = ThreeOnThree(pos, holder: 0);
        s.SetThreePointLine(7.24f, 6.71f);
        Assert.IsTrue(s.IsBeyondArc(pos[1], Rim));
        float corner = TeamMath.ShotValue(s, 1, config);
        s.SetThreePointLine(7.24f, 0f);
        Assert.Greater(corner, TeamMath.ShotValue(s, 1, config) * 1.2f, "the corner line makes it worth three");

        var slots = OffensePlanner.SpacingSlots(RimFloor, new Vector3(0f, 0f, -1f), 7.64f, 5, clearOut: false, maxSide: 7.11f);
        foreach (Vector3 slot in slots) Assert.LessOrEqual(Mathf.Abs(slot.x), 7.11f + 1e-3f);
    }

    // ---------- tactical roles, on-ball spot, hand-placed spacing ----------

    [Test]
    public void Roles_WithBall_OffBall_OnBall_Help()
    {
        var home = new TeamBrain(TeamId.Home, 6, config, rng: new System.Random(1));
        var away = new TeamBrain(TeamId.Away, 6, config, rng: new System.Random(1));
        var s = ThreeOnThree(Standard(), holder: 0);
        var ai = new AIAgentController[6];
        for (int i = 0; i < 6; i++)
        {
            ai[i] = new AIAgentController(config, new System.Random(1), i < 3 ? home : away);
            ai[i].Decide(s, i);
        }
        Assert.AreEqual(TacticalRole.OffenseWithBall, ai[0].CurrentRole);
        Assert.AreEqual(TacticalRole.OffenseOffBall, ai[1].CurrentRole);
        Assert.AreEqual(TacticalRole.DefenseOnBall, ai[3].CurrentRole, "3 guards the handler");
        Assert.AreEqual(TacticalRole.DefenseHelp, ai[4].CurrentRole);
        Assert.AreEqual(TacticalRole.DefenseHelp, ai[5].CurrentRole);
    }

    [Test]
    public void OnBallDefender_GoesBetweenTheHandlerAndTheRim_NotOntoTheBall()
    {
        var pos = Standard();
        pos[3] = new Vector3(1.5f, 0f, 5.5f); // beside the handler (0 at z 5.5), within contest range
        var away = new TeamBrain(TeamId.Away, 6, config, rng: new System.Random(1));
        var ai = new AIAgentController(config, new System.Random(1), away);
        PlayerCommand cmd = ai.Decide(ThreeOnThree(pos, holder: 0), 3);

        Vector3 spot = pos[0] + (RimFloor - pos[0]).normalized * config.contestStandoff;
        Vector3 toSpot = (spot - pos[3]).normalized;
        Vector3 move = new Vector3(cmd.Move.x, 0f, cmd.Move.y);
        // Straight at the handler would be (-1, 0): dot 0.81 with the way to the spot.
        Assert.Greater(Vector3.Dot(move, toSpot), 0.95f, "heads to the spot on the handler-rim line, not at the ball");
    }

    [Test]
    public void SpacingLayout_HandPlacedSpots_AreUsedAwayFromTheBall()
    {
        config.spacingPlayWeight = 1f;
        config.pickAndRollPlayWeight = 0f;
        config.isolationPlayWeight = 0f;
        config.cutTriggerDistance = 99f;
        var layout = ScriptableObject.CreateInstance<SpacingLayout>();
        layout.threePlayers = new List<Vector2> { new Vector2(-5.4f, 5.4f), new Vector2(5.4f, 5.4f), new Vector2(0f, 7.8f) };
        config.spacingLayout = layout;
        var expected = layout.Spots(RimFloor, new Vector3(0f, 0f, -1f), 3);
        var pos = Standard();
        pos[0] = expected[0]; // handler standing on the first spot
        var brain = new TeamBrain(TeamId.Home, 6, config, rng: new System.Random(1));
        brain.Update(ThreeOnThree(pos, holder: 0));

        var targets = new[] { brain.GetOrder(1).Target, brain.GetOrder(2).Target };
        foreach (Vector3 t in targets)
        {
            Assert.IsTrue(TeamMath.FlatDistance(t, expected[1]) < 0.01f || TeamMath.FlatDistance(t, expected[2]) < 0.01f,
                $"{t} is one of the free layout spots");
        }
        Assert.AreNotEqual(targets[0], targets[1]);
    }
}
