namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// The retry budget for fusion configuration downloads. The budget depends on whether a cached
/// fusion configuration exists: with a cache the download gives up quickly and falls back, and
/// without a cache it waits much longer because giving up fails the gateway.
/// </summary>
internal sealed class NitroDownloadRetryPolicy
{
    /// <summary>
    /// Initializes a new instance of <see cref="NitroDownloadRetryPolicy"/>.
    /// </summary>
    /// <param name="attemptsWithCachedSeed">
    /// The number of attempts when a cached fusion configuration exists.
    /// </param>
    /// <param name="attemptsWithoutCachedSeed">
    /// The number of attempts when no cached fusion configuration exists.
    /// </param>
    /// <param name="delay">
    /// The delay between two attempts.
    /// </param>
    public NitroDownloadRetryPolicy(
        int attemptsWithCachedSeed,
        int attemptsWithoutCachedSeed,
        TimeSpan delay)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(attemptsWithCachedSeed, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(attemptsWithoutCachedSeed, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero);

        AttemptsWithCachedSeed = attemptsWithCachedSeed;
        AttemptsWithoutCachedSeed = attemptsWithoutCachedSeed;
        Delay = delay;
    }

    /// <summary>
    /// Gets the number of attempts when a cached fusion configuration exists.
    /// </summary>
    public int AttemptsWithCachedSeed { get; }

    /// <summary>
    /// Gets the number of attempts when no cached fusion configuration exists.
    /// </summary>
    public int AttemptsWithoutCachedSeed { get; }

    /// <summary>
    /// Gets the delay between two attempts.
    /// </summary>
    public TimeSpan Delay { get; }

    /// <summary>
    /// Gets the number of attempts for a download.
    /// </summary>
    /// <param name="hasCachedSeed">
    /// Whether a cached fusion configuration exists.
    /// </param>
    public int GetAttempts(bool hasCachedSeed)
        => hasCachedSeed ? AttemptsWithCachedSeed : AttemptsWithoutCachedSeed;
}
