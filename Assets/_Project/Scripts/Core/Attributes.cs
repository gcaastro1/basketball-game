using System;
using UnityEngine;

namespace Basket.Core
{
    // Basketball attributes from the design brief. Values are fixed and serialized as
    // ints: only ever append new ones (never reorder or reuse numbers).
    public enum AttributeId
    {
        // Offense
        CloseShot = 0, MidRange = 1, ThreePoint = 2, Layup = 3, Dunk = 4, PostScoring = 5,
        BallHandling = 6, Passing = 7, FreeThrow = 8,
        // Defense
        PerimeterDefense = 9, InteriorDefense = 10, Steal = 11, Block = 12, DefensiveRebound = 13,
        // Physical
        Speed = 14, Acceleration = 15, Strength = 16, Vertical = 17, Stamina = 18, Agility = 19,
        // Intelligence
        OffensiveIQ = 20, DefensiveIQ = 21, PassingIQ = 22, ShotSelection = 23, HelpDefense = 24, ReboundPositioning = 25,
    }

    public static class Attributes
    {
        public const int Count = 26;
        public const float Max = 99f;
        // What a player without a character (tests, placeholders) is treated as.
        public const float Neutral = 70f;

        public static float Normalized(float value) => Mathf.Clamp01(value / Max);

        // Normalized attribute of a player, or of a neutral player when there is none.
        public static float N(IPlayerAttributes attributes, AttributeId id) =>
            Normalized(attributes != null ? attributes.Get(id) : Neutral);

        // Linear effect: `low` at 0, `high` at 99.
        public static float Scale(IPlayerAttributes attributes, AttributeId id, float low, float high) =>
            low + (high - low) * N(attributes, id);

        // Multiplier that is exactly 1 at the neutral value (so a neutral player plays
        // like the untuned defaults), `atZero` at 0 and `atMax` at 99, linear in between.
        public static float Centered(float value, float atZero, float atMax)
        {
            if (value <= Neutral) return atZero + (1f - atZero) * Mathf.Clamp01(value / Neutral);
            return 1f + (atMax - 1f) * Mathf.Clamp01((value - Neutral) / (Max - Neutral));
        }

        public static float Centered(IPlayerAttributes attributes, AttributeId id, float atZero, float atMax) =>
            Centered(attributes != null ? attributes.Get(id) : Neutral, atZero, atMax);
    }

    public interface IPlayerAttributes
    {
        float Get(AttributeId id);
    }

    public sealed class AttributeSet : IPlayerAttributes
    {
        private readonly float[] values = new float[Attributes.Count];

        public float Get(AttributeId id) => values[(int)id];
        public void Set(AttributeId id, float value) => values[(int)id] = value;
        public void Add(AttributeId id, float delta) => values[(int)id] += delta;

        public void Clamp(float min, float max)
        {
            for (int i = 0; i < values.Length; i++) values[i] = Mathf.Clamp(values[i], min, max);
        }

        public AttributeSet Clone()
        {
            var copy = new AttributeSet();
            Array.Copy(values, copy.values, values.Length);
            return copy;
        }

        public static AttributeSet Uniform(float value)
        {
            var set = new AttributeSet();
            for (int i = 0; i < Attributes.Count; i++) set.values[i] = value;
            return set;
        }
    }

    public enum ModifierOp { Add, Multiply }

    [Serializable]
    public struct AttributeModifier
    {
        public AttributeId attribute;
        public ModifierOp op;
        public float value;

        public AttributeModifier(AttributeId attribute, ModifierOp op, float value)
        {
            this.attribute = attribute;
            this.op = op;
            this.value = value;
        }
    }

    // How a character likes to play (1 = neutral). Used by the AI; set per character.
    [Serializable]
    public struct AITendencies
    {
        public float threePointPreference;
        public float drivePreference;
        public float passPreference;
        public float stealAggression;
        [Range(0f, 1f)] public float abilityUsage;

        public static AITendencies Neutral => new AITendencies
        {
            threePointPreference = 1f, drivePreference = 1f, passPreference = 1f, stealAggression = 1f, abilityUsage = 0.5f
        };
    }
}
