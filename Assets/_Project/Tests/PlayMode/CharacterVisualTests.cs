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

    private static Bounds ModelBounds(CharacterVisual visual)
    {
        Renderer[] renderers = visual.Model.GetComponentsInChildren<Renderer>();
        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        return b;
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
        Assert.IsTrue(visual.Driver.UsesProcedural, "no clips yet: procedural animation");

        Bounds b = ModelBounds(visual);
        Debug.Log($"Model bounds: height {b.size.y:0.00} m, min y {b.min.y:0.00}, feet {match.Players[0].FeetPosition.y:0.00}");
        Assert.AreEqual(def.modelHeight, b.size.y, 0.25f, "fitted to the configured height (pose changes it a little)");
        Assert.AreEqual(match.Players[0].FeetPosition.y, b.min.y, 0.15f, "stands on the floor");
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
        float right = Vector3.Distance(visual.Driver.RightHandPosition, ball.Position);
        float left = Vector3.Distance(visual.Driver.LeftHandPosition, ball.Position);
        Debug.Log($"Hands to ball: right {right:0.00} m, left {left:0.00} m");
        Assert.Less(right, ball.Radius + 0.12f, "right hand on the ball");
        Assert.Less(left, ball.Radius + 0.12f, "left hand on the ball");
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
}
