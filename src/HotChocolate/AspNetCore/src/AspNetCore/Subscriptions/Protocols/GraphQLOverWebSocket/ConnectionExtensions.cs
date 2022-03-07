namespace HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;

public static class ConnectionExtensions
{
    public static ValueTask CloseInvalidSubscribeMessageAsync(
        this ISocketConnection connection,
        CancellationToken cancellationToken)
        => connection.CloseAsync(
            "Invalid subscribe message structure.",
            CloseReasons.ProtocolError,
            cancellationToken);

    public static ValueTask CloseSubscriptionIdNotUniqueAsync(
        this ISocketConnection connection,
        CancellationToken cancellationToken)
        => connection.CloseAsync(
            "The subscription id is not unique.",
            CloseReasons.SubscriberNotUnique,
            cancellationToken);

    public static ValueTask CloseConnectionInitTimeoutAsync(
        this ISocketConnection connection,
        CancellationToken cancellationToken)
        => connection.CloseAsync(
            "Connection initialization timeout.",
            CloseReasons.ConnectionInitWaitTimeout,
            cancellationToken);

    public static ValueTask CloseInvalidMessageTypeAsync(
        this ISocketConnection connection,
        CancellationToken cancellationToken)
        => connection.CloseAsync(
            "Invalid message type.",
            CloseReasons.ProtocolError,
            cancellationToken);

    public static ValueTask CloseMessageMustBeJsonObjectAsync(
        this ISocketConnection connection,
        CancellationToken cancellationToken)
        => connection.CloseAsync(
            "The message must be a json object.",
            CloseReasons.ProtocolError,
            cancellationToken);

    public static ValueTask CloseMessageTypeIsMandatoryAsync(
        this ISocketConnection connection,
        CancellationToken cancellationToken)
        => connection.CloseAsync(
            "The type property on the message is mandatory.",
            CloseReasons.ProtocolError,
            cancellationToken);

    public static ValueTask CloseUnauthorizedAsync(
        this ISocketConnection connection,
        CancellationToken cancellationToken)
        => connection.CloseAsync(
            "Unauthorized",
            CloseReasons.Unauthorized,
            cancellationToken);

    public static ValueTask CloseConnectionRefusedAsync(
        this ISocketConnection connection,
        CancellationToken cancellationToken)
        => connection.CloseAsync(
            "Connection refused.",
            CloseReasons.Unauthorized,
            cancellationToken);

    public static ValueTask CloseToManyInitializationsAsync(
        this ISocketConnection connection,
        CancellationToken cancellationToken)
        => connection.CloseAsync(
            "Too many initialisation requests.",
            CloseReasons.TooManyInitAttempts,
            cancellationToken);

    public static ValueTask CloseUnexpectedErrorAsync(
        this ISocketConnection connection,
        CancellationToken cancellationToken)
        => connection.CloseAsync(
            "Unexpected Error",
            ConnectionCloseReason.InternalServerError,
            cancellationToken);
}
