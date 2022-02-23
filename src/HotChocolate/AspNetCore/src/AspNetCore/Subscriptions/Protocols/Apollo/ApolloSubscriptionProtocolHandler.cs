using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using HotChocolate.Language;
using static HotChocolate.Language.Utf8GraphQLRequestParser;
using static HotChocolate.AspNetCore.Subscriptions.ProtocolNames;
using static HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo.KeepConnectionAliveMessage;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

internal sealed class ApolloSubscriptionProtocolHandler : IProtocolHandler
{
    private readonly IMessageHandler[] _messageHandlers;

    public ApolloSubscriptionProtocolHandler(IEnumerable<IMessageHandler> messageHandlers)
    {
        if (messageHandlers is null)
        {
            throw new ArgumentNullException(nameof(messageHandlers));
        }

        _messageHandlers = messageHandlers.ToArray();
    }

    public string Name => GraphQL_WS;

    public async Task ExecuteAsync(
        ISocketConnection connection,
        ReadOnlySequence<byte> message,
        CancellationToken cancellationToken)
    {
        try
        {
            if (TryParseMessage(message, out OperationMessage? operationMessage))
            {
                await HandleMessageAsync(connection, operationMessage, cancellationToken);
            }
            else
            {
                await connection.SendAsync(Default, CancellationToken.None);
            }
        }
        catch (WebSocketException)
        {
            // we will just stop receiving
        }
    }

    private static bool TryParseMessage(
        ReadOnlySequence<byte> slice,
        [NotNullWhen(true)] out OperationMessage? message)
    {
        ReadOnlySpan<byte> messageData;
        byte[]? buffer = null;

        if (slice.IsSingleSegment)
        {
            messageData = slice.First.Span;
        }
        else
        {
            buffer = ArrayPool<byte>.Shared.Rent(1024 * 4);
            var buffered = 0;

            SequencePosition position = slice.Start;
            while (slice.TryGet(ref position, out ReadOnlyMemory<byte> memory))
            {
                ReadOnlySpan<byte> span = memory.Span;
                var bytesRemaining = buffer.Length - buffered;

                if (span.Length > bytesRemaining)
                {
                    // TODO : we need to ensure that the message size is restricted like on the
                    // http request.
                    var next = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
                    Buffer.BlockCopy(buffer, 0, next, 0, buffer.Length);
                    ArrayPool<byte>.Shared.Return(buffer);
                    buffer = next;
                }

                for (var i = 0; i < span.Length; i++)
                {
                    buffer[buffered++] = span[i];
                }
            }

            messageData = buffer;
            messageData = messageData.Slice(0, buffered);
        }

        try
        {
            if (messageData.Length == 0 ||
                (messageData.Length == 1 && messageData[0] == default))
            {
                message = null;
                return false;
            }

            GraphQLSocketMessage parsedMessage = ParseMessage(messageData);
            return TryDeserializeMessage(parsedMessage, out message);
        }
        catch (SyntaxException)
        {
            message = null;
            return false;
        }
        finally
        {
            if (buffer is not null)
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }

    private static bool TryDeserializeMessage(
        GraphQLSocketMessage parsedMessage,
        [NotNullWhen(true)] out OperationMessage? message)
    {
        switch (parsedMessage.Type)
        {
            case Messages.ConnectionInitialize:
                message = DeserializeInitConnMessage(parsedMessage);
                return true;

            case Messages.ConnectionTerminate:
                message = TerminateConnectionMessage.Default;
                return true;

            case Messages.Start:
                return TryDeserializeDataStartMessage(parsedMessage, out message);

            case Messages.Stop:
                message = DeserializeDataStopMessage(parsedMessage);
                return true;

            default:
                message = null;
                return false;
        }
    }

    private static InitializeConnectionMessage DeserializeInitConnMessage(
        GraphQLSocketMessage parsedMessage) =>
        parsedMessage.Payload.Length > 0 &&
            ParseJson(parsedMessage.Payload) is IReadOnlyDictionary<string, object?> payload
                ? new InitializeConnectionMessage(payload)
                : new InitializeConnectionMessage();

    private static bool TryDeserializeDataStartMessage(
        GraphQLSocketMessage parsedMessage,
        [NotNullWhen(true)] out OperationMessage? message)
    {
        if (parsedMessage.Payload.Length == 0 || parsedMessage.Id is null)
        {
            message = null;
            return false;
        }

        IReadOnlyList<GraphQLRequest> batch = Parse(parsedMessage.Payload);
        message = new DataStartMessage(parsedMessage.Id, batch[0]);
        return true;
    }

    private static DataStopMessage DeserializeDataStopMessage(
        GraphQLSocketMessage parsedMessage)
    {
        if (parsedMessage.Payload.Length > 0 || parsedMessage.Id is null)
        {
            throw new InvalidOperationException("Invalid message structure.");
        }

        return new DataStopMessage(parsedMessage.Id);
    }

    private async Task HandleMessageAsync(
        ISocketConnection connection,
        OperationMessage message,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < _messageHandlers.Length; i++)
        {
            IMessageHandler handler = _messageHandlers[i];
            if (handler.CanHandle(message))
            {
                await handler.HandleAsync(connection, message, cancellationToken);

                // the message is handled and we are done.
                return;
            }
        }

        throw new NotSupportedException("The specified message type is not supported.");
    }
}
