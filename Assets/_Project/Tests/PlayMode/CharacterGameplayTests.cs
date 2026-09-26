using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Basket.Core;
using Basket.Characters;
using Basket.Gameplay;

// Attributes have to change what happens on the court, not just numbers on a sheet.
public class CharacterGameplayTests
{
    private static AttributeSet With(AttributeId id, float value)
    {
        var a = AttributeSet.Uniform(Attributes.Neutral);
        a.Set(id, value);
        return a;
    }

    private static IEnumerator ShootOnce(float threePoint, System.Action<ShotReport> result)
    {
        using var match = new TestMatch();
        var defaults = ScriptableObject.CreateInstance<ShotConfig>();
        match.ShotConfig.jumpShotBaseError = defaults.jumpShotBaseError;
        match.ShotConfig.jumpShotErrorPerMeter = defaults.jumpShotErrorPerMeter;
        // An apex release would be in the shot meter's green (no aim error at all): this test
        // measures the aim error itself.
        match.ShotConfig.useGreenWindow = false;
        match.Start((TeamId.Home, TestMatch.ShootAtApex()));
        match.Players[0].SetCharacter("Shooter", With(AttributeId.ThreePoint, threePoint), null, AITendencies.Neutral);

        yield return match.RunUntil(() => match.Shots.Count > 0, 3f);
        Assert.AreEqual(1, match.Shots.Count);
        result(match.Shots[0]);
    }

    [UnityTest]
    public IEnumerator ThreePointAttribute_ChangesTheAimErrorOfTheSameShot()
    {
        ShotReport good = default, poor = default;
        yield return ShootOnce(95f, r => good = r);
        yield return ShootOnce(30f, r => poor = r);

        Assert.Greater(good.Distance, 6.75f, "it is a three");
        Assert.Less(good.ErrorRadius, poor.ErrorRadius * 0.7f);
    }

    private static float MeasureJump(PlayerEntity player)
    {
        for (int i = 0; i < 20 && !player.Motor.IsGrounded; i++) player.Motor.Tick(Vector2.zero, false, 0.02f);
        float ground = player.FeetPosition.y;
        player.Motor.Jump();
        float peak = ground;
        for (int i = 0; i < 150; i++)
        {
            player.Motor.Tick(Vector2.zero, false, 0.01f);
            peak = Mathf.Max(peak, player.FeetPosition.y);
        }
        return peak - ground;
    }

    [UnityTest]
    public IEnumerator VerticalAttribute_ChangesJumpHeight()
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.transform.localScale = new Vector3(20f, 0.2f, 20f);
        floor.transform.position = new Vector3(0f, -0.1f, 0f);
        var tuning = ScriptableObject.CreateInstance<AttributeTuning>();
        var movement = ScriptableObject.CreateInstance<PlayerMovementConfig>();

        PlayerEntity high = PlaceholderPlayerFactory.Create("High", TeamId.Home, movement, Color.gray, Color.yellow);
        PlayerEntity low = PlaceholderPlayerFactory.Create("Low", TeamId.Home, movement, Color.gray, Color.yellow);
        low.transform.position += Vector3.right * 5f;
        high.SetCharacter("High", With(AttributeId.Vertical, 95f), null, AITendencies.Neutral);
        low.SetCharacter("Low", With(AttributeId.Vertical, 35f), null, AITendencies.Neutral);
        high.Motor.SetScales(1f, 1f, 1f, high.AttributeMult(AttributeId.Vertical, tuning.verticalJump));
        low.Motor.SetScales(1f, 1f, 1f, low.AttributeMult(AttributeId.Vertical, tuning.verticalJump));
        yield return null;

        float highJump = MeasureJump(high);
        float lowJump = MeasureJump(low);
        Assert.Greater(highJump, lowJump + 0.15f);

        Object.Destroy(high.gameObject);
        Object.Destroy(low.gameObject);
        Object.Destroy(floor);
    }

    [UnityTest]
    public IEnumerator ActiveAbility_UsedBeforeTheShot_ReducesItsError()
    {
        var rainbow = ScriptableObject.CreateInstance<AbilityDefinition>();
        rainbow.displayName = "Test Rainbow";
        rainbow.activation = AbilityActivation.Active;
        rainbow.durationSeconds = 0f;
        rainbow.consumedByShot = true;
        rainbow.shotEffect = new ShotEffect { shots = ShotTypeMask.JumpShot, errorMultiplier = 0.4f, arcHeightMultiplier = 1.5f };

        using var match = new TestMatch();
        var defaults = ScriptableObject.CreateInstance<ShotConfig>();
        match.ShotConfig.jumpShotBaseError = defaults.jumpShotBaseError;
        match.ShotConfig.jumpShotErrorPerMeter = defaults.jumpShotErrorPerMeter;
        // An apex release would be in the shot meter's green (no aim error at all): this test
        // measures the aim error itself.
        match.ShotConfig.useGreenWindow = false;
        var shoot = TestMatch.ShootAtApex();
        bool used = false;
        match.Start((TeamId.Home, (s, self) =>
        {
            PlayerCommand c = shoot(s, self);
            bool press = c.ShootHeld && !used;
            if (press) used = true;
            return new PlayerCommand(c.Move, c.Sprint, c.Pass, c.ShootHeld, ability: press);
        }));
        var attributes = AttributeSet.Uniform(Attributes.Neutral);
        match.Players[0].SetCharacter("Shooter", attributes, new PlayerAbilities(attributes, new[] { rainbow }), AITendencies.Neutral);

        yield return match.RunUntil(() => match.Shots.Count > 0, 3f);
        Assert.IsTrue(match.HasEvent("ABILITY Test Rainbow"), string.Join("\n", match.Events));

        // Same shot, same attributes, no ability: 1 / 0.4 = 2.5x the error.
        var input = new ShotAccuracyInput(ShotType.JumpShot, match.Shots[0].Distance, match.Shots[0].TimingError, match.Shots[0].Contest, 0f,
            Attributes.Normalized(Attributes.Neutral));
        float withoutAbility = ShotAccuracyModel.ErrorRadius(input, match.ShotConfig);
        Assert.AreEqual(withoutAbility * 0.4f, match.Shots[0].ErrorRadius, withoutAbility * 0.05f);
    }
}
