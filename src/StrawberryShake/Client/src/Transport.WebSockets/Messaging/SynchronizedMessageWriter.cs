namespace StrawberryShake.Transport.WebSockets;

/// <summary>
/// Synchronizes the writes to a <see cref="ISocketClient"/> over a
/// <see cref="SocketMessageWriter"/>
/// </summary>
internal sealed class SynchronizedMessageWriter : IAsyncDisposable
{
    private readonly ISocketClient _socketClient;
    private readonly SocketMessageWriter _socketMessageWriter;
    private readonly SemaphoreSlim _semaphore = new(1);

    /// <summary>
    /// Creates a new instance of <see cref="SynchronizedMessageWriter"/>
    /// </summary>
    /// <param name="socketClient">
    /// The socket where the messages should be send to
    /// </param>
    public SynchronizedMessageWriter(ISocketClient socketClient)
    {
        _socketClient = socketClient;
        _socketMessageWriter = new SocketMessageWriter();
    }

    /// <summary>
    /// Sends the changes made in <paramref name="action"/> to the <see cref="ISocketClient"/>
    /// </summary>
    /// <param name="action">Configure the message to send</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operations</param>
    public async ValueTask CommitAsync(
        Action<SocketMessageWriter> action,
        CancellationToken cancellationToken)
    {
        await _semaphore
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            _socketMessageWriter.Reset();
            action.Invoke(_socketMessageWriter);

            await _socketClient
                .SendAsync(_socketMessageWriter.Body, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _semaphore.Dispose();
        await _socketMessageWriter.DisposeAsync().ConfigureAwait(false);
    }
}
