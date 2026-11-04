namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Computes exponentially growing retry delays with random jitter.
/// </summary>
internal static class RabbitMQBackoff
{
    /// <summary>
    /// Gets the delay before the given retry attempt, between 80 % and 120 % of the exponential delay.
    /// </summary>
    /// <param name="attempt">The number of the failed attempt, starting at 1.</param>
    /// <param name="initialDelay">The delay after the first failed attempt.</param>
    /// <param name="maxDelay">The upper bound of the exponential delay before jitter is applied.</param>
    public static TimeSpan GetDelay(int attempt, TimeSpan initialDelay, TimeSpan maxDelay)
    {
        var exponent = Math.Clamp(attempt - 1, 0, 16);
        var delay = Math.Min(initialDelay.TotalMilliseconds * Math.Pow(2, exponent), maxDelay.TotalMilliseconds);

        return TimeSpan.FromMilliseconds(delay * (0.8 + (Random.Shared.NextDouble() * 0.4)));
    }
}
