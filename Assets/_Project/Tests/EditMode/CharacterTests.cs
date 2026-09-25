using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Basket.Core;
using Basket.Characters;

public class CharacterTests
{
    private ProgressionConfig config;
    private CharacterDefinition shooter;

    private sealed class Wallet : IItemWallet
    {
        public readonly Dictionary<string, int> Items = new Dictionary<string, int>();
        public int Count(string id) => Items.TryGetValue(id, out int n) ? n : 0;
        public void Spend(string id, int count) => Items[id] = Count(id) - count;
    }

    [SetUp]
    public void SetUp()
    {
        config = ScriptableObject.CreateInstance<ProgressionConfig>();
        config.limitBreaks[0].costs.Add(new ItemCost("lb_mat_1", 3));
        config.limitBreaks[0].attributeBonus.Add(new AttributeValue(AttributeId.ThreePoint, 5f));
        config.dupes[0].attributeBonus.Add(new AttributeValue(AttributeId.ThreePoint, 2f));
        config.dupes[0].abilityLevelBonus = 1;
        config.dupes[0].cooldownMultiplier = 0.9f;

        shooter = ScriptableObject.CreateInstance<CharacterDefinition>();
        shooter.baseAttributes.Add(new AttributeValue(AttributeId.ThreePoint, 80f));
        shooter.growthPerLevel.Add(new AttributeValue(AttributeId.ThreePoint, 0.2f));
    }

    // ---------- attributes ----------

    [Test]
    public void Centered_IsOneAtNeutralAndHitsTheEnds()
    {
        Assert.AreEqual(1f, Attributes.Centered(Attributes.Neutral, 0.5f, 1.5f), 1e-5f);
        Assert.AreEqual(0.5f, Attributes.Centered(0f, 0.5f, 1.5f), 1e-5f);
        Assert.AreEqual(1.5f, Attributes.Centered(99f, 0.5f, 1.5f), 1e-5f);
        Assert.AreEqual(1f, Attributes.Centered(null, AttributeId.Speed, 0.5f, 1.5f), 1e-5f, "no character = neutral");
    }

    [Test]
    public void Stats_BasePlusGrowthPlusLimitBreakPlusDupes_Clamped()
    {
        var inst = new CharacterInstance("s", level: 11, limitBreak: 1, dupes: 1);
        AttributeSet a = CharacterStatsCalculator.Compute(shooter, inst, config);
        Assert.AreEqual(80f + 0.2f * 10 + 5f + 2f, a.Get(AttributeId.ThreePoint), 1e-4f);
        Assert.AreEqual(CharacterStatsCalculator.DefaultBaseAttribute, a.Get(AttributeId.Dunk), "unset attributes use the default base");

        var maxed = new CharacterInstance("s", level: 60, limitBreak: 1, dupes: 1);
        shooter.growthPerLevel[0] = new AttributeValue(AttributeId.ThreePoint, 5f);
        Assert.AreEqual(config.attributeCap, CharacterStatsCalculator.Compute(shooter, maxed, config).Get(AttributeId.ThreePoint));
    }

    // ---------- levels and limit breaks ----------

    [Test]
    public void LevelCaps_FollowTheLimitBreakStages()
    {
        Assert.AreEqual(20, CharacterProgression.LevelCap(config, 0));
        Assert.AreEqual(40, CharacterProgression.LevelCap(config, 1));
        Assert.AreEqual(50, CharacterProgression.LevelCap(config, 2));
        Assert.AreEqual(60, CharacterProgression.LevelCap(config, 3));
        Assert.AreEqual(60, CharacterProgression.LevelCap(config, 4), "the fourth Limit Break is an awakening");
    }

    [Test]
    public void AddXp_LevelsUpButStopsAtTheCap()
    {
        var inst = new CharacterInstance("s");
        int gained = CharacterProgression.AddXp(inst, config, 10_000_000);
        Assert.AreEqual(19, gained);
        Assert.AreEqual(20, inst.level);
        Assert.AreEqual(0, inst.xp, "excess XP at the cap is discarded");
        Assert.AreEqual(0, CharacterProgression.AddXp(inst, config, 1000));
    }

    [Test]
    public void AddXp_CarriesRemainderToNextLevel()
    {
        var inst = new CharacterInstance("s");
        int toTwo = CharacterProgression.XpToNextLevel(config, 1);
        CharacterProgression.AddXp(inst, config, toTwo + 5);
        Assert.AreEqual(2, inst.level);
        Assert.AreEqual(5, inst.xp);
    }

    [Test]
    public void LimitBreak_RequiresLevelAndMaterials_AndSpendsThem()
    {
        var wallet = new Wallet();
        var inst = new CharacterInstance("s", level: 19);
        Assert.AreEqual(LimitBreakBlock.LevelTooLow, CharacterProgression.CanLimitBreak(inst, config, wallet));

        inst.level = 20;
        Assert.AreEqual(LimitBreakBlock.MissingMaterials, CharacterProgression.CanLimitBreak(inst, config, wallet));

        wallet.Items["lb_mat_1"] = 5;
        Assert.IsTrue(CharacterProgression.LimitBreak(inst, config, wallet));
        Assert.AreEqual(1, inst.limitBreak);
        Assert.AreEqual(2, wallet.Count("lb_mat_1"));
        Assert.AreEqual(40, CharacterProgression.LevelCap(config, inst.limitBreak));
    }

    [Test]
    public void LimitBreak_AllStagesDone_IsBlocked()
    {
        var inst = new CharacterInstance("s", level: 60, limitBreak: 4);
        Assert.AreEqual(LimitBreakBlock.AllDone, CharacterProgression.CanLimitBreak(inst, config, new Wallet()));
    }

    [Test]
    public void Dupes_BaseAndUpToSixCopies()
    {
        var inst = new CharacterInstance("s");
        for (int i = 0; i < 6; i++) Assert.IsTrue(CharacterProgression.AddDupe(inst, config));
        Assert.IsFalse(CharacterProgression.AddDupe(inst, config));
        Assert.AreEqual(6, inst.dupes);
    }

    [Test]
    public void AbilityLevelAndCooldown_GrowWithLimitBreaksAndDupes()
    {
        var inst = new CharacterInstance("s", level: 20, limitBreak: 1, dupes: 1);
        Assert.AreEqual(1 + 1 + 1, CharacterStatsCalculator.AbilityLevel(inst, config));
        Assert.AreEqual(0.9f, CharacterStatsCalculator.CooldownMultiplier(inst, config), 1e-5f);
    }

    [Test]
    public void Abilities_UnlockAtTheirLimitBreak()
    {
        var early = ScriptableObject.CreateInstance<AbilityDefinition>();
        var late = ScriptableObject.CreateInstance<AbilityDefinition>();
        shooter.abilities.Add(new AbilityUnlock { ability = early, unlockAtLimitBreak = 0 });
        shooter.abilities.Add(new AbilityUnlock { ability = late, unlockAtLimitBreak = 2 });

        Assert.AreEqual(1, CharacterStatsCalculator.UnlockedAbilities(shooter, new CharacterInstance("s", limitBreak: 1)).Count);
        Assert.AreEqual(2, CharacterStatsCalculator.UnlockedAbilities(shooter, new CharacterInstance("s", limitBreak: 2)).Count);
    }

    // ---------- abilities at runtime ----------

    private static AbilityDefinition Zone()
    {
        var a = ScriptableObject.CreateInstance<AbilityDefinition>();
        a.displayName = "Zone";
        a.activation = AbilityActivation.Active;
        a.cooldownSeconds = 20f;
        a.durationSeconds = 5f;
        a.modifiers.Add(new AttributeModifier(AttributeId.ThreePoint, ModifierOp.Add, 10f));
        return a;
    }

    [Test]
    public void ActiveAbility_BoostsForItsDuration_ThenCoolsDown()
    {
        var abilities = new PlayerAbilities(AttributeSet.Uniform(70f), new[] { Zone() });
        abilities.Tick(new AbilityContext(0f, -1f, 0, 0));
        Assert.AreEqual(70f, abilities.Get(AttributeId.ThreePoint));

        Assert.IsNotNull(abilities.TryActivate(1f));
        abilities.Tick(new AbilityContext(3f, -1f, 0, 0));
        Assert.AreEqual(80f, abilities.Get(AttributeId.ThreePoint));
        Assert.AreEqual(70f, abilities.Get(AttributeId.Dunk), "only its own attribute");
        Assert.IsNull(abilities.TryActivate(3f), "cooling down");

        abilities.Tick(new AbilityContext(7f, -1f, 0, 0));
        Assert.AreEqual(70f, abilities.Get(AttributeId.ThreePoint), "effect over");
        Assert.IsTrue(abilities.CanActivate(21.1f));
    }

    [Test]
    public void AbilityLevel_ScalesTheEffect()
    {
        var abilities = new PlayerAbilities(AttributeSet.Uniform(70f), new[] { Zone() }, abilityLevel: 3);
        abilities.TryActivate(0f);
        abilities.Tick(new AbilityContext(1f, -1f, 0, 0));
        Assert.AreEqual(70f + 10f * 1.2f, abilities.Get(AttributeId.ThreePoint), 1e-4f);
    }

    [Test]
    public void CooldownMultiplier_ShortensTheCooldown()
    {
        var abilities = new PlayerAbilities(AttributeSet.Uniform(70f), new[] { Zone() }, cooldownMultiplier: 0.5f);
        abilities.TryActivate(0f);
        Assert.IsTrue(abilities.CanActivate(10.1f));
    }

    [Test]
    public void ClutchPassive_OnlyInTheLastMinuteOfACloseGame()
    {
        var clutch = ScriptableObject.CreateInstance<AbilityDefinition>();
        clutch.activation = AbilityActivation.Passive;
        clutch.condition = AbilityCondition.Clutch;
        clutch.modifiers.Add(new AttributeModifier(AttributeId.MidRange, ModifierOp.Add, 8f));
        var abilities = new PlayerAbilities(AttributeSet.Uniform(70f), new[] { clutch });

        abilities.Tick(new AbilityContext(0f, 300f, 0, 0));
        Assert.AreEqual(70f, abilities.Get(AttributeId.MidRange));
        abilities.Tick(new AbilityContext(0f, 30f, -2, 0));
        Assert.AreEqual(78f, abilities.Get(AttributeId.MidRange));
        abilities.Tick(new AbilityContext(0f, 30f, -8, 0));
        Assert.AreEqual(70f, abilities.Get(AttributeId.MidRange), "not close");
    }

    [Test]
    public void NextShotAbility_AppliesToOneMatchingShotOnly()
    {
        var rainbow = ScriptableObject.CreateInstance<AbilityDefinition>();
        rainbow.activation = AbilityActivation.Active;
        rainbow.durationSeconds = 0f;
        rainbow.consumedByShot = true;
        rainbow.shotEffect = new ShotEffect { shots = ShotTypeMask.JumpShot, errorMultiplier = 0.5f, arcHeightMultiplier = 1.6f };
        var abilities = new PlayerAbilities(AttributeSet.Uniform(70f), new[] { rainbow });

        abilities.TryActivate(0f);
        Assert.AreEqual(1f, abilities.TakeShotEffect(ShotType.Layup).errorMultiplier, "wrong shot type");
        ShotEffect e = abilities.TakeShotEffect(ShotType.JumpShot);
        Assert.AreEqual(0.5f, e.errorMultiplier, 1e-5f);
        Assert.AreEqual(1.6f, e.arcHeightMultiplier, 1e-5f);
        Assert.AreEqual(1f, abilities.TakeShotEffect(ShotType.JumpShot).errorMultiplier, "used up");
    }
}
