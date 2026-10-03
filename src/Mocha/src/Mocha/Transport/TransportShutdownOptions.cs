namespace Mocha;

/// <summary>
/// Options that bound cancellation grace and transport cleanup during shutdown.
/// </summary>
public class TransportShutdownOptions : IReadOnlyTransportShutdownOptions
{
    private static readonly TimeSpan s_maxTimeout = TimeSpan.FromMilliseconds(int.MaxValue);

    /// <inheritdoc />
    /// <remarks>Defaults to 5 seconds.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the value is negative or exceeds <see cref="int.MaxValue"/> milliseconds.
    /// </exception>
    public TimeSpan CancellationGracePeriod
    {
        get;
        set
        {
            EnsureValid(value);
            field = value;
        }
    } = TimeSpan.FromSeconds(5);

    /// <inheritdoc />
    /// <remarks>Defaults to 5 seconds.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the value is negative or exceeds <see cref="int.MaxValue"/> milliseconds.
    /// </exception>
    public TimeSpan CleanupTimeout
    {
        get;
        set
        {
            EnsureValid(value);
            field = value;
        }
    } = TimeSpan.FromSeconds(5);

    private static void EnsureValid(TimeSpan value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, s_maxTimeout);
    }
}
