using HotChocolate.Subscriptions.Diagnostics;

namespace HotChocolate.AspNetCore.Subscriptions;

public sealed class SubscriptionTestDiagnostics : SubscriptionDiagnosticEventsListener
{
    private readonly object _sync = new();
    private readonly List<(int Count, TaskCompletionSource Signal)> _subscribedSignals = [];
    private readonly List<(int Count, TaskCompletionSource Signal)> _unsubscribeSignals = [];
    private readonly List<(int Count, TaskCompletionSource Signal)> _closeSignals = [];
    private int _subscribed;
    private int _unsubscribeCount;
    private int _closeCount;

    public int Subscribed => _subscribed;

    public bool UnsubscribeInvoked { get; private set; }

    public bool CloseInvoked { get; private set; }

    public override void SubscribeSuccess(string topicName)
    {
        var subscribed = Interlocked.Increment(ref _subscribed);
        SignalCompleted(_subscribedSignals, subscribed);
    }

    public override void Unsubscribe(string topicName, int shard, int subscribers)
    {
        UnsubscribeInvoked = true;
        var unsubscribeCount = Interlocked.Increment(ref _unsubscribeCount);
        SignalCompleted(_unsubscribeSignals, unsubscribeCount);
    }

    public override void Close(string topicName)
    {
        CloseInvoked = true;
        var closeCount = Interlocked.Increment(ref _closeCount);
        SignalCompleted(_closeSignals, closeCount);
    }

    public Task WaitForSubscribedAsync(CancellationToken cancellationToken)
        => WaitForSubscribedAsync(1, cancellationToken);

    public Task WaitForSubscribedAsync(int count, CancellationToken cancellationToken)
        => WaitForSignalAsync(_subscribedSignals, count, _subscribed, cancellationToken);

    public Task WaitForUnsubscribeAsync(CancellationToken cancellationToken)
        => WaitForUnsubscribeAsync(1, cancellationToken);

    public Task WaitForUnsubscribeAsync(int count, CancellationToken cancellationToken)
        => WaitForSignalAsync(_unsubscribeSignals, count, _unsubscribeCount, cancellationToken);

    public Task WaitForCloseAsync(CancellationToken cancellationToken)
        => WaitForCloseAsync(1, cancellationToken);

    public Task WaitForCloseAsync(int count, CancellationToken cancellationToken)
        => WaitForSignalAsync(_closeSignals, count, _closeCount, cancellationToken);

    private Task WaitForSignalAsync(
        List<(int Count, TaskCompletionSource Signal)> signals,
        int count,
        int currentCount,
        CancellationToken cancellationToken)
    {
        if (currentCount >= count)
        {
            return Task.CompletedTask;
        }

        TaskCompletionSource signal;

        lock (_sync)
        {
            if (GetCurrentCount(signals) >= count)
            {
                return Task.CompletedTask;
            }

            signal = new(TaskCreationOptions.RunContinuationsAsynchronously);
            signals.Add((count, signal));
        }

        return signal.Task.WaitAsync(cancellationToken);
    }

    private int GetCurrentCount(List<(int Count, TaskCompletionSource Signal)> signals)
        => ReferenceEquals(signals, _subscribedSignals)
            ? _subscribed
            : ReferenceEquals(signals, _unsubscribeSignals)
                ? _unsubscribeCount
                : _closeCount;

    private void SignalCompleted(List<(int Count, TaskCompletionSource Signal)> signals, int count)
    {
        TaskCompletionSource[] ready;

        lock (_sync)
        {
            ready = [.. signals.Where(t => t.Count <= count).Select(t => t.Signal)];
            signals.RemoveAll(t => t.Count <= count);
        }

        foreach (var signal in ready)
        {
            signal.TrySetResult();
        }
    }
}
