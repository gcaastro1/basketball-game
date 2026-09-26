using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Basket.Characters;
using Basket.Core;
using Basket.Gameplay;
using Basket.Presentation;

// Etapa 6: the Tripo character replaces the capsule without changing gameplay.
public class CharacterVisualTests
{
    private const string VisualPath = "Assets/_Project/Data/Characters/DefaultCharacterVisual.asset";

    private static CharacterVisualDefinition LoadVisual()
    {
#if UNITY_EDITOR
        var visual = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterVisualDefinition>(VisualPath);
        Assert.IsNotNull(visual, VisualPath);
        Assert.IsNotNull(visual.modelPrefab, "the visual points at the Tripo model");
        return visual;
#else
        Assert.Ignore("needs the editor asset database");
        return null;
#endif
    }


    [UnityTest]
    public IEnumerator Model_ReplacesTheCapsule_FitsTheHeightAndStandsOnTheFloor()
    {
        CharacterVisualDefinition def = LoadVisual();
        using var match = new TestMatch();
        match.Start((TeamId.Home, TestMatch.Idle));
        CharacterVisual visual = CharacterVisual.Attach(match.Players[0], match.Sim, def, Color.blue);
        yield return match.RunUntil(() => false, 0.3f);

        Assert.IsNotNull(visual.Model);
        Assert.IsFalse(match.Players[0].transform.Find("Body").GetComponent<Renderer>().enabled, "capsule hidden");
        var animator = visual.Model.GetComponentInChildren<Animator>();
        Assert.IsTrue(animator.avatar != null && animator.avatar.isHuman, "humanoid avatar");
        // Clips when the character has them (bound in the editor), procedural otherwise.
        Debug.Log($"Animation backend: {(visual.Driver.UsesClips ? "clips (procedural for the rest)" : "procedural")}");
        Assert.AreEqual(def.clips != null && def.clips.HasLocomotion, visual.Driver.UsesClips);

        Assert.IsTrue(CharacterVisual.MeasureMeshY(visual.Model, out float minY, out float maxY));
        float feet = match.Players[0].FeetPosition.y;
        float head = animator.GetBoneTransform(HumanBodyBones.Head).position.y - feet;
        Debug.Log($"Model mesh: height {maxY - minY:0.00} m, min y {minY:0.00}, feet {feet:0.00}, head bone {head:0.00} m above feet");
        Assert.AreEqual(def.modelHeight, maxY - minY, 0.25f, "fitted to the configured height (pose changes it a little)");
        Assert.AreEqual(feet, minY, 0.08f, "stands on the floor");
        // Independent of the mesh measurement: the skeleton ends up at a human height.
        Assert.That(head, Is.InRange(0.7f * def.modelHeight, 1.0f * def.modelHeight), "head bone at a plausible height");
    }

    // The core promise of Etapa 6: visuals are presentation only.
    [UnityTest]
    public IEnumerator Gameplay_IsTheSameWithOrWithoutTheModel()
    {
        CharacterVisualDefinition def = LoadVisual();
        ShotReport plain, modeled;
        int plainScore, modeledScore;
        Vector3 plainRelease, modeledRelease;

        using (var match = new TestMatch())
        {
            match.Start((TeamId.Home, TestMatch.ShootAtApex()));
            yield return match.RunUntil(() => match.Sim.Match.State.ScoreHome > 0, 6f);
            plain = match.Shots[0];
            plainScore = match.Sim.Match.State.ScoreHome;
            plainRelease = match.Players[0].FeetPosition;
        }
        yield return null;
        using (var match = new TestMatch())
        {
            match.Start((TeamId.Home, TestMatch.ShootAtApex()));
            CharacterVisual.Attach(match.Players[0], match.Sim, def, Color.blue);
            yield return match.RunUntil(() => match.Sim.Match.State.ScoreHome > 0, 6f);
            modeled = match.Shots[0];
            modeledScore = match.Sim.Match.State.ScoreHome;
            modeledRelease = match.Players[0].FeetPosition;
        }

        Assert.AreEqual(plainScore, modeledScore);
        Assert.AreEqual(plain.Type, modeled.Type);
        Assert.AreEqual(plain.Distance, modeled.Distance, 0.02f);
        Assert.AreEqual(plain.ErrorRadius, modeled.ErrorRadius, 1e-4f);
        Assert.Less(Vector3.Distance(plainRelease, modeledRelease), 0.05f, "the body moved the same");
    }

    [UnityTest]
    public IEnumerator HoldingTheBall_BothHandsAreOnIt()
    {
        CharacterVisualDefinition def = LoadVisual();
        using var match = new TestMatch();
        match.Start((TeamId.Home, TestMatch.Idle));
        CharacterVisual visual = CharacterVisual.Attach(match.Players[0], match.Sim, def, Color.blue);
        yield return match.RunUntil(() => false, 0.5f);

        Assert.AreEqual(AnimPose.HoldBall, visual.Driver.CurrentPose);
        BallController ball = match.Sim.Ball;
        Animator animator = visual.Model.GetComponentInChildren<Animator>();
        float right = Vector3.Distance(visual.Driver.RightHandPosition, ball.Position);
        float left = Vector3.Distance(visual.Driver.LeftHandPosition, ball.Position);
        // Gameplay carries the ball on the right; the far hand may physically not reach it.
        // Then IK must bring it as close as the arm allows.
        float rightGap = ReachGap(animator, right: true, ball);
        float leftGap = ReachGap(animator, right: false, ball);
        Debug.Log($"Hands to ball: right {right:0.00} m (out of reach by {rightGap:0.00}), left {left:0.00} m (out of reach by {leftGap:0.00})");
        const float onBall = 0.12f;
        Assert.Less(right, ball.Radius + onBall + rightGap, "right hand on the ball (or as close as it reaches)");
        Assert.Less(left, ball.Radius + onBall + leftGap, "left hand on the ball (or as close as it reaches)");
        Assert.Less(rightGap, 0.05f, "the ball is within the carrying (right) arm's reach");
    }

    // How far the ball's surface is beyond this arm's full reach (0 if reachable).
    private static float ReachGap(Animator animator, bool right, BallController ball)
    {
        Transform upper = animator.GetBoneTransform(right ? HumanBodyBones.RightUpperArm : HumanBodyBones.LeftUpperArm);
        Transform lower = animator.GetBoneTransform(right ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm);
        Transform hand = animator.GetBoneTransform(right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
        float arm = Vector3.Distance(upper.position, lower.position) + Vector3.Distance(lower.position, hand.position);
        return Mathf.Max(0f, Vector3.Distance(upper.position, ball.Position) - ball.Radius - arm);
    }

    [UnityTest]
    public IEnumerator RunningPlayer_SwingsTheLegs()
    {
        CharacterVisualDefinition def = LoadVisual();
        using var match = new TestMatch();
        match.Start(
            (TeamId.Home, TestMatch.Idle),
            (TeamId.Home, (s, self) => new PlayerCommand(new Vector2(1f, 0f))));
        CharacterVisual visual = CharacterVisual.Attach(match.Players[1], match.Sim, def, Color.blue);
        Animator animator = visual.Model.GetComponentInChildren<Animator>();
        Transform root = match.Players[1].transform;
        Transform left = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        Transform right = animator.GetBoneTransform(HumanBodyBones.RightFoot);

        float min = float.MaxValue, max = float.MinValue;
        float elapsed = 0f;
        while (elapsed < 1f)
        {
            yield return null;
            match.Sim.Tick(Time.deltaTime);
            elapsed += Time.deltaTime;
            // Feet apart along the running direction (root forward).
            float gap = Vector3.Dot(left.position - right.position, root.forward);
            min = Mathf.Min(min, gap);
            max = Mathf.Max(max, gap);
        }

        Assert.AreEqual(AnimPose.Locomotion, visual.Driver.CurrentPose);
        Debug.Log($"Stride: feet gap {min:0.00}..{max:0.00} m");
        Assert.Greater(max - min, 0.25f, "legs swing past each other");
    }

    // Dribbling while running: the legs keep running (upper-body layer when the dribble is
    // a clip) and the hand stays with the ball.
    [UnityTest]
    public IEnumerator Dribbler_KeepsRunning_WithTheHandOnTheBall()
    {
        CharacterVisualDefinition def = LoadVisual();
        using var match = new TestMatch();
        match.Start((TeamId.Home, (s, self) => new PlayerCommand(new Vector2(0f, 1f))));
        CharacterVisual visual = CharacterVisual.Attach(match.Players[0], match.Sim, def, Color.blue);
        Animator animator = visual.Model.GetComponentInChildren<Animator>();
        Transform root = match.Players[0].transform;
        Transform left = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        Transform right = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        BallController ball = match.Sim.Ball;

        float min = float.MaxValue, max = float.MinValue, closest = float.MaxValue;
        int dribbleFrames = 0;
        float elapsed = 0f;
        while (elapsed < 1.2f)
        {
            yield return null;
            match.Sim.Tick(Time.deltaTime);
            elapsed += Time.deltaTime;
            if (visual.Driver.CurrentPose != AnimPose.Dribble) continue;
            dribbleFrames++;
            float gap = Vector3.Dot(left.position - right.position, root.forward);
            min = Mathf.Min(min, gap);
            max = Mathf.Max(max, gap);
            closest = Mathf.Min(closest, Vector3.Distance(visual.Driver.RightHandPosition, ball.Position));
        }

        Debug.Log($"Dribble run ({(visual.Driver.UsesClips ? "clips" : "procedural")}): {dribbleFrames} frames, feet gap {min:0.00}..{max:0.00} m, hand-ball closest {closest:0.00} m");
        Assert.Greater(dribbleFrames, 10, "the ball handler dribbles while moving");
        Assert.Greater(max - min, 0.25f, "legs keep running while dribbling");
        Assert.Less(closest, ball.Radius + 0.15f, "the hand meets the ball at the top of the bounce");
    }
}
