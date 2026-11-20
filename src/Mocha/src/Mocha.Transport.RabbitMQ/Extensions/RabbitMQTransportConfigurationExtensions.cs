namespace Mocha.Transport.RabbitMQ;

internal static class RabbitMQTransportConfigurationExtensions
{
    /// <summary>
    /// The longest time a timer waits.
    /// </summary>
    private static readonly TimeSpan s_maxTimeout = TimeSpan.FromMilliseconds(uint.MaxValue - 1);

    /// <summary>
    /// Throws when the start timeout is outside its supported range. A value in range is between zero and
    /// 4,294,967,294 milliseconds, or <see cref="Timeout.InfiniteTimeSpan"/>.
    /// </summary>
    public static void ValidateStartTimeout(this RabbitMQTransportConfiguration configuration)
    {
        if (configuration.StartTimeout is { } startTimeout
            && startTimeout != Timeout.InfiniteTimeSpan
            && (startTimeout < TimeSpan.Zero || startTimeout > s_maxTimeout))
        {
            throw ThrowHelper.StartTimeoutOutOfRange(startTimeout);
        }
    }
}
