using System;
using System.Collections.Generic;

namespace GameBalap.Core
{
    public static class GameConstants
    {
        // Lane Dimensions
        public const float LANE_WIDTH = 2.2f;
        public const float LANE_CHANGE_SPEED = 14f;

        // Runner Physics Defaults
        public const float DEFAULT_RUN_SPEED = 12f;
        public const float MAX_RUN_SPEED = 20f;
        public const float SPEED_ACCELERATION = 0.15f;
        public const float JUMP_FORCE = 8.5f;
        public const float GRAVITY = -24f;
        public const float SLIDE_DURATION = 0.75f;
        public const float STUMBLE_DURATION = 1.0f;
        public const float STUMBLE_SPEED_PENALTY = 0.5f;

        // Track Dimensions
        public const float SEGMENT_LENGTH = 30f;
        public const int DEFAULT_TRACK_SEGMENTS = 15; // Sekitar 450 meter balapan (1-2 menit)
        
        // Economy & Tiers
        public const int INITIAL_TEST_COINS = 1500;
        public const int FAUCET_TEST_AMOUNT = 500;

        public static readonly Dictionary<RaceTier, float> TIER_MULTIPLIERS = new Dictionary<RaceTier, float>
        {
            { RaceTier.Bronze, 2.0f },
            { RaceTier.Silver, 2.2f },
            { RaceTier.Gold, 2.5f }
        };

        public static float GetMultiplier(RaceTier tier)
        {
            return TIER_MULTIPLIERS.TryGetValue(tier, out float mult) ? mult : 2.0f;
        }

        public static int CalculatePayout(RaceTier tier)
        {
            int entryFee = (int)tier;
            float multiplier = GetMultiplier(tier);
            return (int)Math.Round(entryFee * multiplier);
        }
    }
}
