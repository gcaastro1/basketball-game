using System;
using System.Collections.Generic;
using UnityEngine;
using Basket.Core;

namespace Basket.Characters
{
    public enum AbilityActivation { Passive, Active }

    // When a passive is on. Active abilities ignore this.
    public enum AbilityCondition
    {
        Always,
        // Last minute of a close game.
        Clutch,
        // After consecutive made shots.
        OnFire,
    }

    [Flags]
    public enum ShotTypeMask { None = 0, JumpShot = 1, Layup = 2, Dunk = 4, FreeThrow = 8, All = 15 }

    [Serializable]
    public struct ShotEffect
    {
        public ShotTypeMask shots;
        // Multiplies the aim error (0.7 = 30% more accurate).
        public float errorMultiplier;
        // Multiplies the shot's arc height (special trajectories).
        public float arcHeightMultiplier;

        public static ShotEffect None => new ShotEffect { shots = ShotTypeMask.None, errorMultiplier = 1f, arcHeightMultiplier = 1f };

        public bool Applies(ShotType type) => (shots & ToMask(type)) != 0;

        public static ShotTypeMask ToMask(ShotType type) => type switch
        {
            ShotType.JumpShot => ShotTypeMask.JumpShot,
            ShotType.Layup => ShotTypeMask.Layup,
            ShotType.Dunk => ShotTypeMask.Dunk,
            ShotType.FreeThrow => ShotTypeMask.FreeThrow,
            _ => ShotTypeMask.None
        };
    }

    // One ability: when it is on and what it changes. Final abilities are undecided
    // (brief section 3); this is the framework, sample assets are placeholders.
    [CreateAssetMenu(fileName = "Ability", menuName = "Basket/Ability Definition")]
    public class AbilityDefinition : ScriptableObject
    {
        public string abilityId = "ability";
        public string displayName = "Placeholder";
        [TextArea] public string description;
        public AbilityActivation activation = AbilityActivation.Active;
        public AbilityCondition condition = AbilityCondition.Always;
        public float cooldownSeconds = 30f;
        // 0 for an active ability that only arms the next shot.
        public float durationSeconds = 6f;
        // The shot effect is used up by the next matching shot.
        public bool consumedByShot;
        public List<AttributeModifier> modifiers = new List<AttributeModifier>();
        public ShotEffect shotEffect = ShotEffect.None;
        // Each ability level above 1 scales every effect by this much (0.1 = +10%).
        public float magnitudePerLevel = 0.1f;
    }

    public readonly struct AbilityContext
    {
        public readonly float Time;
        // Negative when there is no game clock.
        public readonly float GameClock;
        public readonly int ScoreDifference;
        public readonly int ConsecutiveMakes;

        public AbilityContext(float time, float gameClock, int scoreDifference, int consecutiveMakes)
        {
            Time = time;
            GameClock = gameClock;
            ScoreDifference = scoreDifference;
            ConsecutiveMakes = consecutiveMakes;
        }
    }

    // A player's abilities at runtime. Implements IPlayerAttributes: the character's
    // attributes with every currently active ability applied, so gameplay and AI read
    // one number per attribute.
    public sealed class PlayerAbilities : IPlayerAttributes
    {
        public const float ClutchSeconds = 60f;
        public const int ClutchMaxDifference = 3;
        public const int OnFireMakes = 2;

        private sealed class Slot
        {
            public AbilityDefinition Definition;
            public float ActiveUntil = float.NegativeInfinity;
            public float ReadyAt;
            public bool Armed;
            public bool PassiveOn;
        }

        private readonly AttributeSet baseAttributes;
        private readonly List<Slot> slots = new List<Slot>();
        private readonly float level;
        private readonly float cooldownMultiplier;
        private readonly float attributeCap;
        private AbilityContext context;

        public PlayerAbilities(AttributeSet baseAttributes, IEnumerable<AbilityDefinition> abilities, int abilityLevel = 1,
            float cooldownMultiplier = 1f, float attributeCap = 120f)
        {
            this.baseAttributes = baseAttributes;
            level = Mathf.Max(1, abilityLevel);
            this.cooldownMultiplier = cooldownMultiplier;
            this.attributeCap = attributeCap;
            if (abilities != null)
            {
                foreach (var a in abilities) if (a != null) slots.Add(new Slot { Definition = a });
            }
        }

        public IPlayerAttributes Base => baseAttributes;
        public int Count => slots.Count;
        public float Magnitude(AbilityDefinition a) => 1f + a.magnitudePerLevel * (level - 1f);

        public void Tick(AbilityContext ctx)
        {
            context = ctx;
            foreach (Slot s in slots)
            {
                if (s.Definition.activation != AbilityActivation.Passive) continue;
                s.PassiveOn = ConditionMet(s.Definition.condition, ctx);
            }
        }

        public static bool ConditionMet(AbilityCondition condition, AbilityContext ctx) => condition switch
        {
            AbilityCondition.Always => true,
            AbilityCondition.Clutch => ctx.GameClock >= 0f && ctx.GameClock <= ClutchSeconds && Mathf.Abs(ctx.ScoreDifference) <= ClutchMaxDifference,
            AbilityCondition.OnFire => ctx.ConsecutiveMakes >= OnFireMakes,
            _ => false
        };

        public bool IsActive(int index)
        {
            Slot s = slots[index];
            return s.Definition.activation == AbilityActivation.Passive
                ? s.PassiveOn
                : context.Time < s.ActiveUntil || s.Armed;
        }

        public bool CanActivate(float time)
        {
            foreach (Slot s in slots)
            {
                if (s.Definition.activation == AbilityActivation.Active && time >= s.ReadyAt && !s.Armed && time >= s.ActiveUntil) return true;
            }
            return false;
        }

        // Activates the first ready active ability. Returns it, or null.
        public AbilityDefinition TryActivate(float time)
        {
            foreach (Slot s in slots)
            {
                AbilityDefinition d = s.Definition;
                if (d.activation != AbilityActivation.Active || time < s.ReadyAt || s.Armed || time < s.ActiveUntil) continue;
                s.ActiveUntil = time + d.durationSeconds;
                s.Armed = d.consumedByShot;
                s.ReadyAt = time + d.cooldownSeconds * cooldownMultiplier;
                context = new AbilityContext(time, context.GameClock, context.ScoreDifference, context.ConsecutiveMakes);
                return d;
            }
            return null;
        }

        public float Get(AttributeId id)
        {
            float value = baseAttributes.Get(id);
            float add = 0f, mult = 1f;
            for (int i = 0; i < slots.Count; i++)
            {
                if (!IsActive(i)) continue;
                AbilityDefinition d = slots[i].Definition;
                float m = Magnitude(d);
                foreach (AttributeModifier mod in d.modifiers)
                {
                    if (mod.attribute != id) continue;
                    if (mod.op == ModifierOp.Add) add += mod.value * m;
                    else mult *= 1f + (mod.value - 1f) * m;
                }
            }
            return Mathf.Clamp((value + add) * mult, 0f, attributeCap);
        }

        // Combined effect of every active ability on a shot of this type; consumes
        // "next shot" abilities.
        public ShotEffect TakeShotEffect(ShotType type)
        {
            ShotEffect result = ShotEffect.None;
            for (int i = 0; i < slots.Count; i++)
            {
                if (!IsActive(i)) continue;
                Slot s = slots[i];
                ShotEffect e = s.Definition.shotEffect;
                if (!e.Applies(type)) continue;
                float m = Magnitude(s.Definition);
                result.errorMultiplier *= Mathf.Max(0.05f, 1f + (e.errorMultiplier - 1f) * m);
                result.arcHeightMultiplier *= 1f + (e.arcHeightMultiplier - 1f) * m;
                result.shots |= e.shots;
                if (s.Definition.consumedByShot) s.Armed = false;
            }
            return result;
        }
    }
}
