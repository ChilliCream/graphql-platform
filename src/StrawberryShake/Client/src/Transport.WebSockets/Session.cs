using System.Collections.Concurrent;
using StrawberryShake.Properties;
using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Transport.WebSockets;

/// <inheritdoc />
public sealed class Session : ISession
{
    private readonly ISocketClient _socketClient;
    private ISocketProtocol? _socketProtocol;
    private readonly ConcurrentDictionary<string, SocketOperation> _operations = new();

    private bool _disposed;

    /// <summary>
    /// Creates a new instance <see cref="Session"/>
    /// </summary>
    public Session(ISocketClient socketClient)
    {
        _socketClient = socketClient
            ?? throw new ArgumentNullException(nameof(socketClient));

        _socketClient.OnConnectionClosed += ReceiveFinishHandler;
    }

    /// <inheritdoc />
    public string Name => _socketClient.Name;

    /// <inheritdoc />
    public Task<ISocketOperation> StartOperationAsync(
        OperationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return StartOperationAsyncInternal(request, cancellationToken);
    }

    private async Task<ISocketOperation> StartOperationAsyncInternal(
        OperationRequest request,
        CancellationToken cancellationToken)
    {
        EnsureSession(out var socketProtocol);

        var operation = new SocketOperation(this);
        if (_operations.TryAdd(operation.Id, operation))
        {
            try
            {
                socketProtocol.Disposed += (_, _) => BeginStopOperation(operation.Id);

                await socketProtocol
                    .StartOperationAsync(operation.Id, request, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                _operations.TryRemove(operation.Id, out _);
                throw;
            }
        }
        else
        {
            throw ThrowHelper.SessionManager_OperationWasAlreadyRegistered(operation.Id);
        }

        return operation;
    }

    private void BeginStopOperation(string operationId) =>
        Task.Run(async () => await StopOperationAsync(operationId));

    /// <inheritdoc />
    public async Task StopOperationAsync(
        string operationId,
        CancellationToken cancellationToken = default)
    {
        EnsureSession(out var socketProtocol);

        if (_operations.TryRemove(operationId, out var operation))
        {
            await socketProtocol
                .StopOperationAsync(operationId, cancellationToken)
                .ConfigureAwait(false);

            await operation.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async ValueTask CompleteOperation(CancellationToken cancellationToken)
    {
        foreach (var operation in _operations)
        {
            await operation.Value.CompleteAsync(cancellationToken);
        }
    }

    private void ReceiveFinishHandler(object? sender, EventArgs e)
        => _ = CompleteOperation(default);

    /// <summary>
    /// Opens a session over the socket
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation</param>
    public async Task OpenSessionAsync(CancellationToken cancellationToken = default)
    {
        var socketProtocol =
            await _socketClient.OpenAsync(cancellationToken)
                .ConfigureAwait(false);

        _socketProtocol = socketProtocol ??
            throw ThrowHelper.SessionManager_SocketWasNotInitialized(_socketClient.Name);

        _socketProtocol.Subscribe(ReceiveMessage);
    }

    /// <summary>
    /// Closes a session over the socket
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation</param>
    public async Task CloseSessionAsync(CancellationToken cancellationToken = default)
    {
        await _socketClient.CloseAsync(
                Resources.SocketClient_AllOperationsFinished,
                SocketCloseStatus.NormalClosure,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Receive a message from the socket
    /// </summary>
    /// <param name="operationId">Id of the operation</param>
    /// <param name="message">The payload of the message</param>
    /// <param name="cancellationToken">
    /// The cancellation token to cancel
    /// </param>
    private async ValueTask ReceiveMessage(
        string operationId,
        OperationMessage message,
        CancellationToken cancellationToken = default)
    {
        if (_operations.TryGetValue(operationId, out var operation))
        {
            await operation.ReceiveMessageAsync(message, cancellationToken);
        }
    }

    private void EnsureSession(out ISocketProtocol socketProtocol)
    {
        socketProtocol = _socketProtocol
            ?? throw ThrowHelper.SessionManager_SessionIsNotOpen();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            if (_operations.Count > 0)
            {
                var operations = _operations.Values.ToArray();

                for (var i = 0; i < operations.Length; i++)
                {
                    await operations[i].DisposeAsync();
                }

                _operations.Clear();
            }

            _socketClient.OnConnectionClosed -= ReceiveFinishHandler;
            _socketProtocol?.Unsubscribe(ReceiveMessage);
            await _socketClient.DisposeAsync();
        }
    }
}
