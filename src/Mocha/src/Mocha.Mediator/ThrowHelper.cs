namespace Mocha.Mediator;

internal static class ThrowHelper
{
    public static Exception MissingPipeline(Type messageType)
        => new InvalidOperationException(
            $"No pipeline registered for message type {messageType}");

    public static Exception MissingNotificationPipeline(Type notificationType)
        => new InvalidOperationException(
            $"No notification pipeline registered for message type {notificationType}. "
            + "If this is a command or query, use SendAsync or QueryAsync instead.");

    public static Exception BeforeAndAfterConflict()
        => new ArgumentException(
            "Only one of 'before' or 'after' can be specified at the same time.");

    public static Exception MiddlewareKeyNotFound(string key)
        => new InvalidOperationException(
            $"The middleware with the key `{key}` was not found.");

    public static Exception UnknownHandlerKind(MediatorHandlerKind kind)
        => new InvalidOperationException(
            $"Unknown handler kind: {kind}");

    public static Exception HandlerInterfaceNotFound(Type handlerType)
        => new InvalidOperationException(
            $"Type '{handlerType}' does not implement any known mediator handler interface.");

    public static Exception MultipleHandlerInterfaces(Type handlerType)
        => new InvalidOperationException(
            $"Type '{handlerType}' implements multiple mediator handler interfaces. "
            + "A handler must implement exactly one of ICommandHandler<>, IQueryHandler<,>, or INotificationHandler<>.");
}
