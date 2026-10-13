using Mocha.Middlewares;
using Mocha.Utils;

namespace Mocha;

/// <summary>
/// Holds the messages a consumer attempt dispatches until the attempt completes, then releases
/// them to their transports in dispatch order.
/// </summary>
/// <param name="services">The service provider of the scope the messages are dispatched from.</param>
/// <param name="runtime">The messaging runtime that dispatches the held messages.</param>
/// <param name="pools">The pools providing the dispatch contexts of the released messages.</param>
internal sealed class DeferredDispatchScope(
    IServiceProvider services,
    IMessagingRuntime runtime,
    IMessagingPools pools)
{
    private readonly List<PendingDispatch> _pending = [];

    /// <summary>
    /// Gets whether dispatches are currently held back.
    /// </summary>
    public bool IsOpen { get; private set; }

    /// <summary>
    /// Gets whether held messages are waiting to be released.
    /// </summary>
    public bool HasPending => _pending.Count > 0;

    /// <summary>
    /// Starts holding dispatches.
    /// </summary>
    public void Open() => IsOpen = true;

    /// <summary>
    /// Stops holding dispatches and drops any message that was not released.
    /// </summary>
    public void Close()
    {
        IsOpen = false;
        Discard();
    }

    /// <summary>
    /// Holds a serialized message. The envelope body is copied, so the caller can reuse its buffer.
    /// </summary>
    /// <param name="endpoint">The endpoint the message leaves through.</param>
    /// <param name="messageType">The message type, or <see langword="null"/> for an untyped envelope.</param>
    /// <param name="envelope">The serialized message.</param>
    public void Add(DispatchEndpoint endpoint, MessageType? messageType, MessageEnvelope envelope)
    {
        var body = envelope.Body;
        var buffer = BufferPools.Rent(body.Length);
        body.Span.CopyTo(buffer);

        var pending = new PendingDispatch(
            endpoint,
            messageType,
            new MessageEnvelope(envelope) { Body = buffer.AsMemory(0, body.Length) },
            buffer);

        lock (_pending)
        {
            _pending.Add(pending);
        }
    }

    /// <summary>
    /// Releases the held messages in the order they were added. A failure stops the release and
    /// drops the messages that have not left yet.
    /// </summary>
    /// <param name="cancellationToken">The token that cancels the release.</param>
    public async ValueTask FlushAsync(CancellationToken cancellationToken)
    {
        if (_pending.Count == 0)
        {
            return;
        }

        var pool = pools.DispatchContext;

        try
        {
            for (var i = 0; i < _pending.Count; i++)
            {
                var pending = _pending[i];
                var context = pool.Get();

                try
                {
                    // The envelope already carries the ids, so the context takes them over instead of
                    // generating a new set.
                    context.MessageId = pending.Envelope.MessageId;
                    context.CorrelationId = pending.Envelope.CorrelationId;
                    context.ConversationId = pending.Envelope.ConversationId;
                    context.Initialize(services, pending.Endpoint, runtime, pending.MessageType, cancellationToken);
                    context.Envelope = pending.Envelope;

                    await pending.Endpoint.DispatchDeferredAsync(context);
                }
                finally
                {
                    pool.Return(context);
                }
            }
        }
        finally
        {
            Discard();
        }
    }

    /// <summary>
    /// Drops the held messages and returns their buffers.
    /// </summary>
    public void Discard()
    {
        lock (_pending)
        {
            foreach (var pending in _pending)
            {
                BufferPools.Return(pending.Buffer);
            }

            _pending.Clear();
        }
    }

    private readonly record struct PendingDispatch(
        DispatchEndpoint Endpoint,
        MessageType? MessageType,
        MessageEnvelope Envelope,
        byte[] Buffer);
}
