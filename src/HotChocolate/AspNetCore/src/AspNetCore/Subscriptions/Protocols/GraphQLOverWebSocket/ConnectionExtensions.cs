namespace HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;

public static class ConnectionExtensions
{
    public static Task CloseInvalidSubscribeMessageAsync(
        this ISocketConnection connection,
        CancellationToken cancellationToken)
        => connection.CloseAsync(
            "Invalid subscribe message structure.",
            CloseReasons.ProtocolError,
            cancellationToken);

    public static Task CloseSubscriptionIdNotUniqueAsync(
        this ISocketConnection connection,
        CancellationToken cancellationToken)
        => connection.CloseAsync(
            "The subscription id is not unique.",
            CloseReasons.SubscriberNotUnique,
            cancellationToken);

    public static Task CloseConnectionInitTimeoutAsync(
        this ISocketConnection connection,
        CancellationToken cancellationToken)
        => connection.CloseAsync(
            "Connection initialization timeout.",
            CloseReasons.ConnectionInitWaitTimeout,
            cancellationToken);

    public static Task CloseInvalidMessageTypeAsync(
        this ISocketConnection connection,
        CancellationToken cancellationToken)
        => connection.CloseAsync(
            "Invalid message type.",
            CloseReasons.ProtocolError,
            cancellationToken);

    public static Task CloseMessageMustBeJsonObjectAsync(
        this ISocketConnection connection,
        CancellationToken cancellationToken)
        => connection.CloseAsync(
            "The message must be a json object.",
            CloseReasons.ProtocolError,
            cancellationToken);

    public static Task CloseMessageTypeIsMandatoryAsync(
        this ISocketConnection connection,
        CancellationToken cancellationToken)
        => connection.CloseAsync(
            "The type property on the message is mandatory.",
            CloseReasons.ProtocolError,
            cancellationToken);

    public static Task CloseUnauthorizedAsync(
        this ISocketConnection connection,
        CancellationToken cancellationToken)
        => connection.CloseAsync(
            "Unauthorized",
            CloseReasons.Unauthorized,
            cancellationToken);

    public static Task CloseConnectionRefusedAsync(
        this ISocketConnection connection,
        CancellationToken cancellationToken)
        => connection.CloseAsync(
            "Connection refused.",
            CloseReasons.Unauthorized,
            cancellationToken);

    public static Task CloseToManyInitializationsAsync(
        this ISocketConnection connection,
        CancellationToken cancellationToken)
        => connection.CloseAsync(
            "Too many initialisation requests.",
            CloseReasons.TooManyInitAttempts,
            cancellationToken);

    public static Task CloseUnexpectedErrorAsync(
        this ISocketConnection connection,
        CancellationToken cancellationToken)
        => connection.CloseAsync(
            "Unexpected Error",
            ConnectionCloseReason.InternalServerError,
            cancellationToken);
}
