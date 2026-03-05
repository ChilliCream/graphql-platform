namespace Mocha.Threading;

/// <summary>
/// Implements an exponential backoff strategy with jitter to manage retry delays.
/// </summary>
/// <remarks>
/// Uses a randomized multiplier derived from the base delay to avoid the thundering herd problem
/// when multiple consumers retry simultaneously. The delay doubles with each attempt, capped at
/// a configurable maximum. Call <see cref="Reset"/> after a successful operation to restart the
/// backoff sequence.
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
    /// <param name="delay">The base delay used to compute the randomized multiplier for each backoff interval.</param>
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
    /// Computes the next backoff delay using the current power level and a randomized jitter multiplier.
    /// </summary>
    /// <returns>
    /// A <see cref="TimeSpan"/> representing the delay, clamped to the configured maximum delay.
    /// </returns>
    public TimeSpan CalculateDelay()
    {
        // we use a random multiplier because we don't want to have a thundering herd problem
        var multiplier = Random.Shared.Next(_delayInMs / 2, _delayInMs);
        if (multiplier == 0)
        {
            multiplier = 1;
        }

        var waitTimeInMs = Math.Pow(2, _power) * multiplier;

        // we make sure that we don't exceed the max delay
        return TimeSpan.FromMilliseconds(Math.Min(_maxDelayInMs, waitTimeInMs));
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
