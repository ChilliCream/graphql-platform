using Mocha.Outbox;

namespace Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;

/// <summary>
/// An outbox signal whose <see cref="Set"/> never throws
/// <see cref="ObjectDisposedException"/>. The production
/// <c>MessageBusOutboxSignal</c> wraps <c>AsyncAutoResetEvent</c> which
/// throws on <c>Set()</c> after disposal. In integration tests the outbox
/// processor's own transaction commits fire the EF Core interceptor that
/// calls <c>Set()</c>, and this can race with provider disposal.
/// <para>
/// Uses a <c>TaskCompletionSource</c> to implement auto-reset semantics.
/// Unlike <c>SemaphoreSlim</c>, this approach does not accumulate waiters
/// across loop iterations - each <c>WaitAsync</c> atomically exchanges
/// the TCS, and <c>Set</c> signals whoever is currently waiting.
/// </para>
/// </summary>
internal sealed class ResilientOutboxSignal : IOutboxSignal
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private TaskCompletionSource _tcs;

    public ResilientOutboxSignal()
    {
        // Start signaled (like production AsyncAutoResetEvent(true))
        _tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _tcs.TrySetResult();
    }

    public void Set()
    {
        lock (_lock)
        {
            if (!_tcs.TrySetResult())
            {
                // TCS was already cancelled or faulted - replace with a
                // pre-completed TCS so the next WaitAsync sees the signal.
                _tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _tcs.TrySetResult();
            }
        }
    }

    public Task WaitAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            var task = _tcs.Task;

            if (task.IsCompletedSuccessfully)
            {
                // Reset: replace with a new, unsignaled TCS
                _tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                return Task.CompletedTask;
            }

            if (task.IsCanceled || task.IsFaulted)
            {
                // Previous waiter was cancelled - replace with fresh TCS
                _tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            // Register cancellation on the current TCS
            var tcs = _tcs;
            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(static state => ((TaskCompletionSource)state!).TrySetCanceled(), tcs);
            }

            return tcs.Task;
        }
    }
}
