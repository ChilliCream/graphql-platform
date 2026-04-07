namespace Mocha;

/// <summary>
/// Provides delay calculation logic for the redelivery middleware.
/// </summary>
internal static class RedeliveryExecutor
{
    internal static TimeSpan CalculateDelay(int attempt, RedeliveryPolicyConfig config)
    {
        TimeSpan baseDelay;

        if (config.Intervals is { Length: > 0 } intervals)
        {
            // Explicit intervals: use array index, clamp to last.
            baseDelay = intervals[Math.Min(attempt, intervals.Length - 1)];
        }
        else
        {
            // Calculated: BaseDelay * (attempt + 1).
            var configuredBaseDelay = config.BaseDelay ?? RedeliveryPolicyDefaults.Intervals[0];
            baseDelay = configuredBaseDelay * (attempt + 1);
        }

        // Cap by MaxDelay.
        var maxDelay = config.MaxDelay ?? RedeliveryPolicyDefaults.MaxDelay;

        if (baseDelay > maxDelay)
        {
            baseDelay = maxDelay;
        }

        // Add jitter: +/- 25%.
        var useJitter = config.UseJitter ?? RedeliveryPolicyDefaults.UseJitter;

        if (useJitter)
        {
            var jitterRange = baseDelay.TotalMilliseconds * 0.25;
            var jitter = ((Random.Shared.NextDouble() * 2) - 1) * jitterRange;
            baseDelay = TimeSpan.FromMilliseconds(Math.Max(0, baseDelay.TotalMilliseconds + jitter));
        }

        return baseDelay;
    }
}
