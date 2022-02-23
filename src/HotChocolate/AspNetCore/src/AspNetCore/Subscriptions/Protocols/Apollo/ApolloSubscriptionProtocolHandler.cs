using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using HotChocolate.Language;
using static HotChocolate.Language.Utf8GraphQLRequestParser;
using static HotChocolate.AspNetCore.Subscriptions.ProtocolNames;
using static HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo.KeepConnectionAliveMessage;
using static HotChocolate.AspNetCore.Subscriptions.Protocols.MessageUtilities;

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
        ReadOnlySequence<byte> body,
        [NotNullWhen(true)] out OperationMessage? message)
    {
        message = null;
        return MessageUtilities.TryParseMessage(body, out GraphQLSocketMessage parsed) &&
            TryDeserializeMessage(parsed, out message);
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
