namespace Mocha;

/// <summary>
/// The cancellation of a batch handler. It is cancelled when the consumer stops, or when the receives of
/// all messages in the batch are cancelled.
/// </summary>
internal sealed class BatchCancellation : IDisposable
{
    private readonly CancellationTokenSource _source;
    private readonly CancellationTokenRegistration[] _registrations;
    private int _remainingReceives;

    private BatchCancellation(CancellationToken consumerToken, List<CancellationToken> receiveTokens)
    {
        _source = CancellationTokenSource.CreateLinkedTokenSource(consumerToken);
        _registrations = new CancellationTokenRegistration[receiveTokens.Count];
        _remainingReceives = receiveTokens.Count;

        for (var i = 0; i < receiveTokens.Count; i++)
        {
            _registrations[i] = receiveTokens[i].UnsafeRegister(
                static state => ((BatchCancellation)state!).OnReceiveCancelled(),
                this);
        }
    }

    /// <summary>
    /// Gets the token passed to the batch handler.
    /// </summary>
    public CancellationToken Token => _source.Token;

    /// <summary>
    /// Creates the cancellation for <paramref name="batch"/>. A message whose receive cannot be
    /// cancelled keeps the batch running until <paramref name="consumerToken"/> is cancelled.
    /// </summary>
    public static BatchCancellation Create<TEvent>(MessageBatch<TEvent> batch, CancellationToken consumerToken)
    {
        // The messages of a batch usually come from one endpoint and share its receive token.
        var receiveTokens = new List<CancellationToken>(1);

        foreach (var entry in batch.Entries)
        {
            var token = entry.Context.CancellationToken;

            if (!token.CanBeCanceled)
            {
                receiveTokens.Clear();
                break;
            }

            if (!receiveTokens.Contains(token))
            {
                receiveTokens.Add(token);
            }
        }

        return new BatchCancellation(consumerToken, receiveTokens);
    }

    private void OnReceiveCancelled()
    {
        if (Interlocked.Decrement(ref _remainingReceives) == 0)
        {
            _source.Cancel();
        }
    }

    public void Dispose()
    {
        foreach (var registration in _registrations)
        {
            registration.Dispose();
        }

        _source.Dispose();
    }
}
