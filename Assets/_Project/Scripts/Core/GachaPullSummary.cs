using System.Collections.Generic;

namespace Basket.Core
{
    // Mesma ideia de MatchRewardSummary, pro resultado de um pull de gacha (PullOutcome é
    // Basket.Meta, não cruza pra UI).
    public readonly struct GachaPullSummary
    {
        public readonly bool Success;
        public readonly string FailureReason;
        public readonly IReadOnlyList<string> ResultDescriptions;

        public GachaPullSummary(bool success, string failureReason, IReadOnlyList<string> resultDescriptions)
        {
            Success = success;
            FailureReason = failureReason;
            ResultDescriptions = resultDescriptions;
        }
    }
}
