using Microsoft.CodeAnalysis;

namespace Mocha.Analyzers;

/// <summary>
/// Provides <see cref="DiagnosticDescriptor"/> definitions for all diagnostics reported
/// by the Mocha mediator source generator.
/// </summary>
public static class Errors
{
    /// <summary>
    /// Gets the descriptor for MO0001: a message type has no registered handler.
    /// </summary>
    /// <remarks>
    /// Reported as a warning when a command or query type is declared but no corresponding
    /// handler implementation is found in the compilation.
    /// </remarks>
    public static readonly DiagnosticDescriptor MissingHandler = new(
        id: "MO0001",
        title: "Missing handler for message type",
        messageFormat: "Message type '{0}' has no registered handler",
        category: "Mediator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// Gets the descriptor for MO0002: a message type has more than one handler.
    /// </summary>
    /// <remarks>
    /// Reported as an error when a command or query type has multiple handler implementations.
    /// Commands and queries must have exactly one handler; notifications are excluded from this rule.
    /// </remarks>
    public static readonly DiagnosticDescriptor DuplicateHandler = new(
        id: "MO0002",
        title: "Duplicate handler for message type",
        messageFormat: "Message type '{0}' has multiple handlers: {1}",
        category: "Mediator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Gets the descriptor for MO0003: a handler class is abstract and cannot be registered.
    /// </summary>
    /// <remarks>
    /// Reported as a warning when a class implements a handler interface but is declared
    /// <see langword="abstract"/>, preventing it from being instantiated at runtime.
    /// </remarks>
    public static readonly DiagnosticDescriptor AbstractHandler = new(
        id: "MO0003",
        title: "Handler is abstract",
        messageFormat: "Handler '{0}' is abstract and will not be registered",
        category: "Mediator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// Gets the descriptor for MO0004: a message type is an open generic and cannot be dispatched.
    /// </summary>
    /// <remarks>
    /// Reported as an info when a command or query type has unbound type parameters,
    /// making it impossible to dispatch at runtime.
    /// </remarks>
    public static readonly DiagnosticDescriptor OpenGenericMessageType = new(
        id: "MO0004",
        title: "Open generic message type cannot be dispatched",
        messageFormat: "Message type '{0}' is an open generic and cannot be dispatched at runtime",
        category: "Mediator",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    /// <summary>
    /// Gets the descriptor for MO0005: a handler type implements multiple mediator handler interfaces.
    /// </summary>
    /// <remarks>
    /// Reported as an error when a concrete class implements more than one of
    /// <c>ICommandHandler</c>, <c>IQueryHandler</c>, or <c>INotificationHandler</c>.
    /// A handler must implement exactly one mediator handler interface.
    /// </remarks>
    public static readonly DiagnosticDescriptor MultipleHandlerInterfaces = new(
        id: "MO0005",
        title: "Handler implements multiple mediator handler interfaces",
        messageFormat: "Handler '{0}' must implement exactly one mediator handler interface",
        category: "Mediator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // --- MessageBus diagnostics ---

    /// <summary>
    /// Gets the descriptor for MO0010: a request type has no registered handler.
    /// </summary>
    /// <remarks>
    /// Reported as a warning when an event request type is declared but no corresponding
    /// handler implementation is found in the compilation.
    /// </remarks>
    public static readonly DiagnosticDescriptor MissingRequestHandler = new(
        id: "MO0010",
        title: "Missing handler for request type",
        messageFormat: "Request type '{0}' has no registered handler",
        category: "Messaging",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// Gets the descriptor for MO0011: a request type has more than one handler.
    /// </summary>
    /// <remarks>
    /// Reported as an error when a request type has multiple handler implementations.
    /// Request types must have exactly one handler.
    /// </remarks>
    public static readonly DiagnosticDescriptor DuplicateRequestHandler = new(
        id: "MO0011",
        title: "Duplicate handler for request type",
        messageFormat: "Request type '{0}' has multiple handlers: {1}",
        category: "Messaging",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Gets the descriptor for MO0012: a messaging handler is an open generic and cannot be auto-registered.
    /// </summary>
    /// <remarks>
    /// Reported as an info when a messaging handler has unbound type parameters,
    /// making it impossible to register at compile time.
    /// </remarks>
    public static readonly DiagnosticDescriptor OpenGenericMessagingHandler = new(
        id: "MO0012",
        title: "Open generic messaging handler cannot be auto-registered",
        messageFormat: "Handler '{0}' is an open generic and cannot be auto-registered",
        category: "Messaging",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    /// <summary>
    /// Gets the descriptor for MO0013: a messaging handler class is abstract and cannot be registered.
    /// </summary>
    /// <remarks>
    /// Reported as a warning when a class implements a messaging handler interface but is declared
    /// abstract, preventing it from being instantiated at runtime.
    /// </remarks>
    public static readonly DiagnosticDescriptor AbstractMessagingHandler = new(
        id: "MO0013",
        title: "Messaging handler is abstract",
        messageFormat: "Handler '{0}' is abstract and will not be registered",
        category: "Messaging",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// Gets the descriptor for MO0014: a saga subclass has no public parameterless constructor.
    /// </summary>
    /// <remarks>
    /// Reported as an error when a <c>Saga&lt;TState&gt;</c> subclass does not have a public
    /// parameterless constructor, which is required by the <c>new()</c> constraint on <c>AddSaga&lt;T&gt;</c>.
    /// </remarks>
    public static readonly DiagnosticDescriptor SagaMissingParameterlessConstructor = new(
        id: "MO0014",
        title: "Saga must have a public parameterless constructor",
        messageFormat: "Saga '{0}' must have a public parameterless constructor",
        category: "Messaging",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
