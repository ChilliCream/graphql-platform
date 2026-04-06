using System.Diagnostics.CodeAnalysis;

namespace Mocha.Mediator;

/// <summary>
/// Extension methods for <see cref="MediatorMiddlewareFactoryContext"/> that allow middleware
/// factories to inspect the pipeline being compiled and decide whether to participate.
/// Returning <c>next</c> from a middleware factory when these checks fail eliminates the
/// middleware from that pipeline entirely - zero runtime cost.
/// </summary>
public static class MediatorMiddlewareFactoryContextExtensions
{
    /// <summary>
    /// Returns <see langword="true"/> if the pipeline is for a void command (<see cref="ICommand"/>).
    /// </summary>
    public static bool IsCommand(this MediatorMiddlewareFactoryContext context)
        => typeof(ICommand).IsAssignableFrom(context.MessageType);

    /// <summary>
    /// Returns <see langword="true"/> if the pipeline is for a command with a response (<see cref="ICommand{TResponse}"/>).
    /// </summary>
    public static bool IsCommandWithResponse(this MediatorMiddlewareFactoryContext context)
        => HasGenericInterface(context.MessageType, typeof(ICommand<>));

    /// <summary>
    /// Returns <see langword="true"/> if the pipeline is for a query (<see cref="IQuery{TResponse}"/>).
    /// </summary>
    public static bool IsQuery(this MediatorMiddlewareFactoryContext context)
        => HasGenericInterface(context.MessageType, typeof(IQuery<>));

    /// <summary>
    /// Returns <see langword="true"/> if the pipeline is for a notification (<see cref="INotification"/>).
    /// </summary>
    public static bool IsNotification(this MediatorMiddlewareFactoryContext context)
        => typeof(INotification).IsAssignableFrom(context.MessageType);

    /// <summary>
    /// Returns <see langword="true"/> if the message type is assignable to <typeparamref name="T"/>.
    /// </summary>
    public static bool IsMessageAssignableTo<T>(this MediatorMiddlewareFactoryContext context)
        => typeof(T).IsAssignableFrom(context.MessageType);

    /// <summary>
    /// Returns <see langword="true"/> if the message type is assignable to <paramref name="type"/>.
    /// </summary>
    public static bool IsMessageAssignableTo(this MediatorMiddlewareFactoryContext context, Type type)
        => type.IsAssignableFrom(context.MessageType);

    /// <summary>
    /// Returns <see langword="true"/> if the response type is assignable to <typeparamref name="T"/>.
    /// Returns <see langword="false"/> for void commands and notifications (no response type).
    /// </summary>
    public static bool IsResponseAssignableTo<T>(this MediatorMiddlewareFactoryContext context)
        => context.ResponseType is not null && typeof(T).IsAssignableFrom(context.ResponseType);

    /// <summary>
    /// Returns <see langword="true"/> if the response type is assignable to <paramref name="type"/>.
    /// Returns <see langword="false"/> for void commands and notifications (no response type).
    /// </summary>
    public static bool IsResponseAssignableTo(this MediatorMiddlewareFactoryContext context, Type type)
        => context.ResponseType is not null && type.IsAssignableFrom(context.ResponseType);

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Metadata read on statically-referenced types is AOT-safe.")]
    private static bool HasGenericInterface(Type type, Type openGeneric)
    {
        foreach (var @interface in type.GetInterfaces())
        {
            if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == openGeneric)
            {
                return true;
            }
        }

        return false;
    }
}
