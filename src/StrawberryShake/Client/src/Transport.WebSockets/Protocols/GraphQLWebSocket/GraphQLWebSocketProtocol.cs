using System.Buffers;
using System.Text.Json;
using StrawberryShake.Properties;
using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Transport.WebSockets.Protocols;

/// <summary>
/// A implementation of <see cref="ISocketProtocol"/> that uses graphql-ws protocol to
/// communicate with the server
/// </summary>
public sealed class GraphQLWebSocketProtocol : SocketProtocolBase
{
    private readonly ISocketClient _socketClient;
    private readonly MessagePipeline _receiver;
    private readonly SynchronizedMessageWriter _sender;

    /// <summary>
    /// Initializes a new instance of <see cref="GraphQLWebSocketProtocol"/>
    /// </summary>
    /// <param name="socketClient">
    /// The client where this protocol is using
    /// </param>
    public GraphQLWebSocketProtocol(ISocketClient socketClient)
    {
        _socketClient = socketClient ??
            throw new ArgumentNullException(nameof(socketClient));
        _receiver = new MessagePipeline(socketClient, ProcessAsync);
        _sender = new SynchronizedMessageWriter(socketClient);
    }

    /// <inheritdoc />
    public override async Task StartOperationAsync(
        string operationId,
        OperationRequest request,
        CancellationToken cancellationToken)
    {
        if (_socketClient.IsClosed)
        {
            throw ThrowHelper.Protocol_CannotStartOperationOnClosedSocket(operationId);
        }

        await _sender
            .CommitAsync(
                x => x.WriteStartOperationMessage(operationId, request),
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task StopOperationAsync(
        string operationId,
        CancellationToken cancellationToken)
    {
        if (_socketClient.IsClosed)
        {
            return;
        }

        await _sender
            .CommitAsync(x => x.WriteStopOperationMessage(operationId), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_socketClient.IsClosed)
        {
            throw ThrowHelper.Protocol_CannotInitializeOnClosedSocket();
        }

        var payload =
            await _socketClient
                .ConnectionInterceptor
                .CreateConnectionInitPayload(this, cancellationToken);

        await _sender
            .CommitAsync(x => x.WriteInitializeMessage(payload), cancellationToken)
            .ConfigureAwait(false);

        _receiver.Start();
    }

    /// <inheritdoc />
    public override async Task TerminateAsync(CancellationToken cancellationToken)
    {
        if (_socketClient.IsClosed)
        {
            return;
        }

        await _sender
            .CommitAsync(x => x.WriteTerminateMessage(), cancellationToken)
            .ConfigureAwait(false);

        await _receiver.Stop().ConfigureAwait(false);
    }

    private ValueTask ProcessAsync(
        ReadOnlySequence<byte> slice,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = GraphQLWebSocketMessageParser.Parse(slice);

            if (message.Id is { } id)
            {
                return message.Type switch
                {
                    GraphQLWebSocketMessageType.Data =>
                        Notify(
                            id,
                            new DataDocumentOperationMessage<JsonDocument>(message.Payload),
                            cancellationToken),

                    GraphQLWebSocketMessageType.Complete =>
                        Notify(
                            id,
                            CompleteOperationMessage.Default,
                            cancellationToken),

                    GraphQLWebSocketMessageType.Error =>
                        Notify(
                            id,
                            new ErrorOperationMessage(CreateError(message.Payload)),
                            cancellationToken),

                    GraphQLWebSocketMessageType.ConnectionError =>
                        Notify(
                            id,
                            ErrorOperationMessage.ConnectionInitializationError,
                            cancellationToken),

                    _ => CloseSocketOnProtocolError(
                        "Invalid message type received: " + message.Type,
                        cancellationToken),
                };
            }
        }
        catch (Exception ex)
        {
            return CloseSocketOnProtocolError(
                "Invalid message received: " + ex.Message,
                cancellationToken);
        }

        return default;
    }

    private static IClientError CreateError(JsonDocument error)
    {
        if (error.RootElement.TryGetProperty("message", out var messageProp) &&
            messageProp.GetString() is {  Length: > 0, } message)
        {
            return new ClientError(message);
        }

        throw new SocketOperationException(Resources.ErrorOperationMessage_ResponseParsingError);
    }

    private async ValueTask CloseSocketOnProtocolError(
        string message,
        CancellationToken cancellationToken)
    {
        await _socketClient
            .CloseAsync(message, SocketCloseStatus.ProtocolError, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync().ConfigureAwait(false);
        await _receiver.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }
}
