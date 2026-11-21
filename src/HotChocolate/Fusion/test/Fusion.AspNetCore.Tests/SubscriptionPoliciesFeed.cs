using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace HotChocolate.Fusion;

/// <summary>
/// Hands out the events a test publishes to every subscription the source schema has opened.
/// </summary>
public sealed class SubscriptionPoliciesFeed
{
    private readonly object _sync = new();
    private readonly List<Channel<SubscriptionPoliciesSource.Item>> _channels = [];
    private readonly List<(int Count, TaskCompletionSource Signal)> _waiters = [];
    private int _opened;

    /// <summary>
    /// Gets the number of source schema subscriptions that were opened.
    /// </summary>
    public int Opened
    {
        get
        {
            lock (_sync)
            {
                return _opened;
            }
        }
    }

    /// <summary>
    /// Opens a source schema subscription that yields every published event until it is canceled.
    /// </summary>
    public async IAsyncEnumerable<SubscriptionPoliciesSource.Item> SubscribeAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<SubscriptionPoliciesSource.Item>();

        lock (_sync)
        {
            _channels.Add(channel);
            _opened++;

            foreach (var waiter in _waiters.Where(w => w.Count <= _opened))
            {
                waiter.Signal.TrySetResult();
            }
        }

        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return item;
            }
        }
        finally
        {
            lock (_sync)
            {
                _channels.Remove(channel);
            }
        }
    }

    /// <summary>
    /// Sends an event to every open source schema subscription.
    /// </summary>
    public void Publish(SubscriptionPoliciesSource.Item item)
    {
        lock (_sync)
        {
            foreach (var channel in _channels)
            {
                channel.Writer.TryWrite(item);
            }
        }
    }

    /// <summary>
    /// Waits until the source schema has opened the given number of subscriptions.
    /// </summary>
    public Task WaitForOpenedAsync(int count)
    {
        lock (_sync)
        {
            if (_opened >= count)
            {
                return Task.CompletedTask;
            }

            var signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _waiters.Add((count, signal));

            return signal.Task.WaitAsync(TestContext.Current.CancellationToken);
        }
    }
}
