using System.Threading.Channels;
using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Transport.WebSockets;

/// <summary>
/// Represents a operation on a socket
/// </summary>
public sealed class SocketOperation : ISocketOperation
{
    private readonly ISession _manager;
    private readonly Channel<OperationMessage> _channel;
    private bool _disposed;

    /// <summary>
    /// The id of the operation
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Initializes a new <see cref="SocketOperation"/>
    /// </summary>
    /// <param name="manager">
    /// The socket operation manager that this operation manages
    /// </param>
    public SocketOperation(ISession manager)
        : this(manager, Guid.NewGuid().ToString())
    {
    }

    /// <summary>
    /// Initializes a new <see cref="SocketOperation"/>
    /// </summary>
    /// <param name="manager">
    /// The socket operation manager that this operation manages
    /// </param>
    /// <param name="id">
    /// The id of this operation
    /// </param>
    public SocketOperation(
        ISession manager,
        string id)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        Id = id ?? throw new ArgumentNullException(nameof(id));
        _channel = Channel.CreateUnbounded<OperationMessage>();
    }

    /// <inheritdoc />
    public IAsyncEnumerable<OperationMessage> ReadAsync()
        => new MessageStream(this, _channel);

    /// <inheritdoc />
    public async ValueTask CompleteAsync(CancellationToken cancellationToken)
    {
        if (!_disposed)
        {
            try
            {
                await _channel.Writer
                    .WriteAsync(CompleteOperationMessage.Default, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (ChannelClosedException)
            {
                // if the channel is closed we will move on.
            }
        }
    }

    private sealed class MessageStream(SocketOperation operation, Channel<OperationMessage> channel)
        : IAsyncEnumerable<OperationMessage>
    {
        public async IAsyncEnumerator<OperationMessage> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            if (operation._disposed)
            {
                yield break;
            }

            var reader = channel.Reader;
            while (!operation._disposed && !reader.Completion.IsCompleted)
            {
                if (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)
                    && reader.TryRead(out var message))
                {
                    yield return message;
                }
            }
        }
    }

    internal async ValueTask ReceiveMessageAsync(
        OperationMessage message,
        CancellationToken cancellationToken)
    {
        if (!_disposed)
        {
            try
            {
                await _channel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
            }
            catch (ChannelClosedException)
            {
                // if the channel is closed we will move on.
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _channel.Writer.TryComplete();
            await _manager.StopOperationAsync(Id, CancellationToken.None).ConfigureAwait(false);
            _disposed = true;
        }
    }
}
