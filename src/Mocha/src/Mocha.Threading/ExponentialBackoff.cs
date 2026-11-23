namespace Mocha.Threading;

/// <summary>
/// Implements an exponential backoff strategy with jitter to manage retry delays.
/// </summary>
/// <remarks>
/// The delay doubles with each attempt up to a configurable maximum, and each wait is randomized
/// between half of that value and the full value. Call <see cref="Reset"/> after a successful
/// operation to restart the backoff sequence.
/// </remarks>
public sealed class ExponentialBackoff
{
    private readonly int _maxRetries;
    private readonly int _maxDelayInMs;
    private readonly int _delayInMs;
    private int _retries;
    private int _power;

    /// <summary>
    /// Creates a new exponential backoff instance with the specified retry limits and delay bounds.
    /// </summary>
    /// <param name="maxRetries">The maximum number of retry attempts before <see cref="ShouldRetry"/> returns <c>false</c>.</param>
    /// <param name="delay">The base delay, which doubles with each retry.</param>
    /// <param name="maxDelay">The upper bound on the computed delay; no single wait will exceed this duration.</param>
    public ExponentialBackoff(int maxRetries, TimeSpan delay, TimeSpan maxDelay)
    {
        _maxRetries = maxRetries;
        _maxDelayInMs = (int)maxDelay.TotalMilliseconds;
        _delayInMs = (int)delay.TotalMilliseconds;
        Reset();
    }

    /// <summary>
    /// Gets a value indicating whether another retry attempt is permitted under the configured maximum.
    /// </summary>
    public bool ShouldRetry => _retries < _maxRetries;

    /// <summary>
    /// Increments the retry counter and asynchronously waits for the computed backoff interval.
    /// </summary>
    /// <remarks>
    /// If the <paramref name="cancellationToken"/> is signaled during the delay, the wait
    /// completes immediately without throwing an exception.
    /// </remarks>
    /// <param name="cancellationToken">A token that can cancel the delay without raising an exception.</param>
    /// <returns>A task that completes after the backoff delay or when cancellation is requested.</returns>
    public async Task WaitAsync(CancellationToken cancellationToken)
    {
        _retries++;

        var currentDelay = CalculateDelay();

        _power++;

        try
        {
            await Task.Delay(currentDelay, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // we don't want to throw an exception here
        }
    }

    /// <summary>
    /// Computes the randomized delay for the next retry.
    /// </summary>
    /// <returns>
    /// A <see cref="TimeSpan"/> representing the delay, which never exceeds the configured maximum delay.
    /// </returns>
    public TimeSpan CalculateDelay()
        => CalculateDelay(_power, TimeSpan.FromMilliseconds(_delayInMs), TimeSpan.FromMilliseconds(_maxDelayInMs));

    /// <summary>
    /// Computes a randomized exponential delay for a failed attempt. The result lies between half of
    /// the exponential delay and the exponential delay, which is capped at <paramref name="maxDelay"/>.
    /// </summary>
    /// <param name="attempt">The number of the failed attempt, starting at 1.</param>
    /// <param name="delay">
    /// The base delay, which doubles with each attempt. The first attempt waits between one and two base delays.
    /// </param>
    /// <param name="maxDelay">The upper bound on the computed delay.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="delay"/> or <paramref name="maxDelay"/> is negative.
    /// </exception>
    public static TimeSpan CalculateDelay(int attempt, TimeSpan delay, TimeSpan maxDelay)
    {
        if (delay < TimeSpan.Zero)
        {
            throw ThrowHelper.DelayMustNotBeNegative(nameof(delay), delay);
        }

        if (maxDelay < TimeSpan.Zero)
        {
            throw ThrowHelper.DelayMustNotBeNegative(nameof(maxDelay), maxDelay);
        }

        // delay * 2^attempt
        var exponentialDelayInMs = Math.ScaleB(delay.TotalMilliseconds, Math.Max(attempt, 1));
        var ceilingInMs = Math.Min(maxDelay.TotalMilliseconds, exponentialDelayInMs);

        return TimeSpan.FromMilliseconds(ceilingInMs * (0.5 + Random.Shared.NextDouble() / 2));
    }

    /// <summary>
    /// Resets the retry counter and power level to their initial values so the backoff sequence starts over.
    /// </summary>
    public void Reset()
    {
        _retries = 0;
        _power = 1;
    }
}
