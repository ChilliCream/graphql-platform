namespace Mocha.Analyzers;

/// <summary>
/// Provides CLR metadata names for Mocha mediator types used to resolve
/// <see cref="Microsoft.CodeAnalysis.INamedTypeSymbol"/> instances from a compilation.
/// </summary>
public static class SyntaxConstants
{
    /// <summary>
    /// Gets the metadata name for the <c>ICommandHandler&lt;TCommand&gt;</c> interface (void return).
    /// </summary>
    public const string ICommandHandlerVoid = "Mocha.Mediator.ICommandHandler`1";

    /// <summary>
    /// Gets the metadata name for the <c>ICommandHandler&lt;TCommand, TResponse&gt;</c> interface.
    /// </summary>
    public const string ICommandHandlerResponse = "Mocha.Mediator.ICommandHandler`2";

    /// <summary>
    /// Gets the metadata name for the <c>IQueryHandler&lt;TQuery, TResponse&gt;</c> interface.
    /// </summary>
    public const string IQueryHandler = "Mocha.Mediator.IQueryHandler`2";

    /// <summary>
    /// Gets the metadata name for the <c>INotificationHandler&lt;TNotification&gt;</c> interface.
    /// </summary>
    public const string INotificationHandler = "Mocha.Mediator.INotificationHandler`1";

    /// <summary>
    /// Gets the metadata name for the <c>ICommand</c> marker interface (void return).
    /// </summary>
    public const string ICommand = "Mocha.Mediator.ICommand";

    /// <summary>
    /// Gets the metadata name for the <c>ICommand&lt;TResponse&gt;</c> interface.
    /// </summary>
    public const string ICommandOfT = "Mocha.Mediator.ICommand`1";

    /// <summary>
    /// Gets the metadata name for the <c>IQuery&lt;TResponse&gt;</c> interface.
    /// </summary>
    public const string IQueryOfT = "Mocha.Mediator.IQuery`1";

    /// <summary>
    /// Gets the metadata name for the <c>INotification</c> marker interface.
    /// </summary>
    public const string INotificationMarker = "Mocha.Mediator.INotification";

    /// <summary>
    /// Gets the metadata name for the <c>MediatorModuleAttribute</c> class.
    /// </summary>
    public const string MediatorModuleAttribute = "Mocha.Mediator.MediatorModuleAttribute";

    // --- MessageBus types ---

    /// <summary>
    /// Gets the metadata name for the <c>IEventHandler&lt;TEvent&gt;</c> interface.
    /// </summary>
    public const string IEventHandler = "Mocha.IEventHandler`1";

    /// <summary>
    /// Gets the metadata name for the <c>IEventRequestHandler&lt;TRequest&gt;</c> interface (void return).
    /// </summary>
    public const string IEventRequestHandlerVoid = "Mocha.IEventRequestHandler`1";

    /// <summary>
    /// Gets the metadata name for the <c>IEventRequestHandler&lt;TRequest, TResponse&gt;</c> interface.
    /// </summary>
    public const string IEventRequestHandlerResponse = "Mocha.IEventRequestHandler`2";

    /// <summary>
    /// Gets the metadata name for the <c>IConsumer&lt;TMessage&gt;</c> interface.
    /// </summary>
    public const string IConsumer = "Mocha.IConsumer`1";

    /// <summary>
    /// Gets the metadata name for the <c>IBatchEventHandler&lt;TEvent&gt;</c> interface.
    /// </summary>
    public const string IBatchEventHandler = "Mocha.IBatchEventHandler`1";

    /// <summary>
    /// Gets the metadata name for the <c>Saga&lt;TState&gt;</c> abstract class.
    /// </summary>
    public const string Saga = "Mocha.Sagas.Saga`1";

    /// <summary>
    /// Gets the metadata name for the <c>IEventRequest</c> marker interface (void return).
    /// </summary>
    public const string IEventRequest = "Mocha.IEventRequest";

    /// <summary>
    /// Gets the metadata name for the <c>IEventRequest&lt;TResponse&gt;</c> interface.
    /// </summary>
    public const string IEventRequestOfT = "Mocha.IEventRequest`1";

    /// <summary>
    /// Gets the metadata name for the <c>MessagingModuleAttribute</c> class.
    /// </summary>
    public const string MessagingModuleAttribute = "Mocha.MessagingModuleAttribute";
}
