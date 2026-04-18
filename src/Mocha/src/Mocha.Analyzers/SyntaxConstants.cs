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
    /// Gets the fully qualified display string for the <c>IEventRequest</c> marker interface,
    /// used when comparing emitted <c>typeof(...)</c> expressions.
    /// </summary>
    public const string IEventRequestDisplay = "global::Mocha.IEventRequest";

    /// <summary>
    /// Gets the fully qualified display prefix for a closed <c>IEventRequest&lt;TResponse&gt;</c>,
    /// used to detect framework base types in emitted <c>typeof(...)</c> expressions.
    /// </summary>
    public const string IEventRequestOfTDisplayPrefix = "global::Mocha.IEventRequest<";

    /// <summary>
    /// Gets the metadata name for the <c>MessagingModuleAttribute</c> class.
    /// </summary>
    public const string MessagingModuleAttribute = "Mocha.MessagingModuleAttribute";

    /// <summary>
    /// Gets the named property name for the <c>JsonContext</c> property
    /// on <c>MessagingModuleAttribute</c>.
    /// </summary>
    public const string JsonContextProperty = "JsonContext";

    /// <summary>
    /// Gets the metadata name for the <c>MessagingModuleInfoAttribute</c> class.
    /// </summary>
    public const string MessagingModuleInfoAttribute = "Mocha.MessagingModuleInfoAttribute";

    /// <summary>
    /// Gets the metadata name for the <c>MediatorModuleInfoAttribute</c> class.
    /// </summary>
    public const string MediatorModuleInfoAttribute = "Mocha.Mediator.MediatorModuleInfoAttribute";

    /// <summary>
    /// Gets the named property name for the <c>MessageTypes</c> property
    /// on <c>MessagingModuleInfoAttribute</c> and <c>MediatorModuleInfoAttribute</c>.
    /// </summary>
    public const string MessageTypesProperty = "MessageTypes";

    /// <summary>
    /// Gets the named property name for the <c>SagaTypes</c> property
    /// on <c>MessagingModuleInfoAttribute</c>.
    /// </summary>
    public const string SagaTypesProperty = "SagaTypes";

    /// <summary>
    /// Gets the named property name for the <c>HandlerTypes</c> property
    /// on <c>MessagingModuleInfoAttribute</c> and <c>MediatorModuleInfoAttribute</c>.
    /// </summary>
    public const string HandlerTypesProperty = "HandlerTypes";

    /// <summary>
    /// Gets the metadata name for the <c>IMessageBus</c> interface.
    /// </summary>
    public const string IMessageBus = "Mocha.IMessageBus";

    /// <summary>
    /// Gets the metadata name for the <c>ISender</c> interface (Mediator).
    /// </summary>
    public const string ISender = "Mocha.Mediator.ISender";

    /// <summary>
    /// Gets the metadata name for the <c>IPublisher</c> interface (Mediator).
    /// </summary>
    public const string IPublisher = "Mocha.Mediator.IPublisher";
}
