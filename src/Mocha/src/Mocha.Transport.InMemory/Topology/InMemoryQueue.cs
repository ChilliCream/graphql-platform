using System.Buffers;
using System.Collections.Immutable;
using System.Threading.Channels;
using Mocha.Middlewares;

namespace Mocha.Transport.InMemory;

/// <summary>
/// Represents a InMemory queue entity with its configuration.
/// </summary>
public sealed class InMemoryQueue : TopologyResource<InMemoryQueueConfiguration>, IInMemoryResource
{
    private readonly Channel<InMemoryQueueItem> _channel = Channel.CreateUnbounded<InMemoryQueueItem>();

    private ImmutableArray<InMemoryBinding> _bindings = [];

    /// <summary>
    /// Gets the name that uniquely identifies this queue within the topology.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Gets the bindings that route messages into this queue.
    /// </summary>
    public ImmutableArray<InMemoryBinding> Bindings => _bindings;

    protected override void OnInitialize(InMemoryQueueConfiguration configuration)
    {
        Name = configuration.Name!;
    }

    protected override void OnComplete(InMemoryQueueConfiguration configuration)
    {
        var address = new UriBuilder(Topology.Address);
        address.Path = Path.Combine(address.Path, "q", configuration.Name!);
        Address = address.Uri;
    }

    internal void AddBinding(InMemoryBinding binding)
    {
        ImmutableInterlocked.Update(ref _bindings, (current) => current.Add(binding));
    }

    /// <summary>
    /// Enqueues a message envelope into this queue for consumption by a receive endpoint.
    /// </summary>
    /// <param name="envelope">The message envelope to enqueue.</param>
    /// <param name="cancellationToken">A token to cancel the write operation.</param>
    /// <returns>A task that completes when the envelope has been written to the internal channel.</returns>
    public ValueTask SendAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        var item = InMemoryQueueItem.Create(envelope);
        return _channel.Writer.WriteAsync(item, cancellationToken);
    }

    /// <summary>
    /// Returns an async stream of queued items, blocking until new messages arrive or cancellation is requested.
    /// </summary>
    /// <param name="cancellationToken">A token to stop consuming.</param>
    /// <returns>An async enumerable of <see cref="InMemoryQueueItem"/> instances that must be disposed after processing.</returns>
    public IAsyncEnumerable<InMemoryQueueItem> ConsumeAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}

/// <summary>
/// Wraps a message envelope together with its pooled buffer for zero-copy in-memory transfer.
/// </summary>
/// <remarks>
/// The underlying byte buffer is rented from <see cref="ArrayPool{T}"/> and
/// must be returned by calling <see cref="Dispose"/>. Failing to dispose will cause pool exhaustion.
/// </remarks>
public class InMemoryQueueItem : IDisposable
{
    private readonly byte[] _buffer;
    private readonly MessageEnvelope _envelope;

    /// <summary>
    /// Creates a new queue item backed by the given envelope and pooled buffer.
    /// </summary>
    /// <param name="envelope">The message envelope whose body references <paramref name="buffer"/>.</param>
    /// <param name="buffer">A rented byte array that backs the envelope body and will be returned on dispose.</param>
    public InMemoryQueueItem(MessageEnvelope envelope, byte[] buffer)
    {
        _envelope = envelope;
        _buffer = buffer;
    }

    /// <summary>
    /// Gets the message envelope carried by this queue item.
    /// </summary>
    public MessageEnvelope Envelope => _envelope;

    /// <summary>
    /// Returns the rented buffer to the shared array pool.
    /// </summary>
    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(_buffer);
    }

    /// <summary>
    /// Creates a new <see cref="InMemoryQueueItem"/> by copying the envelope body into a pooled buffer.
    /// </summary>
    /// <param name="envelope">The source envelope whose body will be copied.</param>
    /// <returns>A new queue item that owns the pooled buffer; the caller must dispose it after use.</returns>
    public static InMemoryQueueItem Create(MessageEnvelope envelope)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(envelope.Body.Length);
        envelope.Body.CopyTo(buffer);
        return new InMemoryQueueItem(
            new MessageEnvelope(envelope) { Body = buffer.AsMemory()[..envelope.Body.Length] },
            buffer);
    }
}
