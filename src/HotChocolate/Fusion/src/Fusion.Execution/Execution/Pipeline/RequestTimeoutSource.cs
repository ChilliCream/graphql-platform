using System.Diagnostics;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution.Pipeline;

/// <summary>
/// A reusable cancellation source for one request at a time that is canceled when the request
/// is aborted, when the request runs into its timeout, or when the schema shuts down.
/// </summary>
internal sealed class RequestTimeoutSource : IDisposable
{
    // The state packs a generation into the upper bits and the timeout phase into the lower two
    // bits, so a timer callback that was dispatched for an earlier request cannot claim a later one.
    private const int PhaseMask = 3;
    private const int Idle = 0;
    private const int Armed = 1;
    private const int Fired = 2;
    private const int GenerationIncrement = 4;

    private readonly CancellationTokenSource _source = new();
    private readonly ITimer _timer;
    private readonly CancellationTokenRegistration _shutdownRegistration;
    private CancellationTokenRegistration _abortRegistration;
    private long _deadline;
    private int _state;

    /// <summary>
    /// Initializes a new instance of <see cref="RequestTimeoutSource"/>.
    /// </summary>
    /// <param name="shutdown">
    /// The token that signals that the schema shuts down.
    /// </param>
    public RequestTimeoutSource(CancellationToken shutdown)
    {
        _timer = TimeProvider.System.CreateTimer(
            static state => ((RequestTimeoutSource)state!).OnTimer(),
            this,
            Timeout.InfiniteTimeSpan,
            Timeout.InfiniteTimeSpan);
        _shutdownRegistration = shutdown.UnsafeRegister(
            static state => ((RequestTimeoutSource)state!)._source.Cancel(),
            this);
    }

    /// <summary>
    /// Gets the token that is canceled when the request is aborted, times out or the schema shuts down.
    /// </summary>
    public CancellationToken Token => _source.Token;

    /// <summary>
    /// Gets a value indicating whether the current request ran into its timeout.
    /// </summary>
    public bool IsTimedOut => (Volatile.Read(ref _state) & PhaseMask) == Fired;

    /// <summary>
    /// Starts tracking a request.
    /// </summary>
    /// <param name="requestAborted">
    /// The token that signals that the request was aborted.
    /// </param>
    /// <param name="timeout">
    /// The time after which the request times out.
    /// </param>
    public void Start(CancellationToken requestAborted, TimeSpan timeout)
    {
        _abortRegistration = requestAborted.UnsafeRegister(
            static state => ((RequestTimeoutSource)state!)._source.Cancel(),
            this);

        Volatile.Write(ref _deadline, Stopwatch.GetTimestamp() + ToTimestampTicks(timeout));
        var generation = (Volatile.Read(ref _state) & ~PhaseMask) + GenerationIncrement;
        Volatile.Write(ref _state, generation | Armed);
        _timer.Change(timeout, Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Stops the timeout of the current request. Request abort and schema shutdown are still observed.
    /// </summary>
    public void StopTimeout()
    {
        var state = Volatile.Read(ref _state);

        if ((state & PhaseMask) == Armed
            && Interlocked.CompareExchange(ref _state, (state & ~PhaseMask) | Idle, state) == state)
        {
            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>
    /// Detaches the current request and prepares the source for the next one.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the source can be reused; <c>false</c> if it was canceled.
    /// </returns>
    public bool TryReset()
    {
        StopTimeout();

        _abortRegistration.Dispose();
        _abortRegistration = default;

        return (Volatile.Read(ref _state) & PhaseMask) == Idle && _source.TryReset();
    }

    /// <summary>
    /// Releases the timer and the registrations of this source.
    /// </summary>
    public void Dispose()
    {
        _timer.Dispose();
        _shutdownRegistration.Dispose();
        _abortRegistration.Dispose();
    }

    private void OnTimer()
    {
        var state = Volatile.Read(ref _state);

        if ((state & PhaseMask) != Armed)
        {
            return;
        }

        var remaining = Volatile.Read(ref _deadline) - Stopwatch.GetTimestamp();

        if (remaining > 0)
        {
            // The timer fired before the deadline of the current request, either early or for a
            // previous request, so it is scheduled again for the remaining time.
            _timer.Change(Stopwatch.GetElapsedTime(0, remaining), Timeout.InfiniteTimeSpan);
            return;
        }

        if (Interlocked.CompareExchange(ref _state, (state & ~PhaseMask) | Fired, state) == state)
        {
            _source.Cancel();
        }
    }

    private static long ToTimestampTicks(TimeSpan timeout)
        => (long)(timeout.TotalSeconds * Stopwatch.Frequency);

    /// <summary>
    /// The pool policy that creates sources bound to the schema shutdown token.
    /// </summary>
    public sealed class Policy(CancellationToken shutdown) : PooledObjectPolicy<RequestTimeoutSource>
    {
        /// <inheritdoc />
        public override RequestTimeoutSource Create() => new(shutdown);

        /// <inheritdoc />
        public override bool Return(RequestTimeoutSource obj) => obj.TryReset();
    }
}
