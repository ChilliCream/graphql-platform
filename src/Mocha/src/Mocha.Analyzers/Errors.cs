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

    /// <summary>
    /// Gets the descriptor for MO0015: a messaging module must specify a JsonSerializerContext
    /// when publishing for AOT.
    /// </summary>
    /// <remarks>
    /// Reported as an error when <c>PublishAot</c> is <c>true</c> but the
    /// <c>[assembly: MessagingModule]</c> attribute does not include a <c>JsonContext</c> property.
    /// </remarks>
    public static readonly DiagnosticDescriptor MissingJsonSerializerContext = new(
        id: "MO0015",
        title: "Missing JsonSerializerContext for AOT",
        messageFormat: "MessagingModule '{0}' must specify JsonContext when publishing for AOT. Add JsonContext = typeof(YourJsonContext) to the MessagingModule attribute.",
        category: "Messaging",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Gets the descriptor for MO0016: a message type is not included in the JsonSerializerContext.
    /// </summary>
    /// <remarks>
    /// Reported as an error when a type is used as a message, request, response, or saga state
    /// but is not declared via <c>[JsonSerializable(typeof(...))]</c> on the specified
    /// <c>JsonSerializerContext</c>.
    /// </remarks>
    public static readonly DiagnosticDescriptor MissingJsonSerializable = new(
        id: "MO0016",
        title: "Missing JsonSerializable attribute",
        messageFormat: "Type '{0}' is used as a message type but is not included in JsonSerializerContext '{1}'. Add [JsonSerializable(typeof({0}))] to the context.",
        category: "Messaging",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Gets the descriptor for MO0018: a type used at a call site is not in the JsonSerializerContext.
    /// </summary>
    /// <remarks>
    /// Reported as a warning when AOT publishing is enabled and a message type used in a
    /// dispatch call is not declared via <c>[JsonSerializable(typeof(...))]</c> on the specified
    /// <c>JsonSerializerContext</c>.
    /// </remarks>
    public static readonly DiagnosticDescriptor CallSiteTypeNotInJsonContext = new(
        id: "MO0018",
        title: "Type not in JsonSerializerContext",
        messageFormat: "Type '{0}' is used in a {1} call but is not included in JsonSerializerContext '{2}'. Add [JsonSerializable(typeof({0}))] to the context.",
        category: "Messaging",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// Gets the descriptor for MO0020: a command or query is sent but no handler was found.
    /// </summary>
    /// <remarks>
    /// Reported as a warning when a command or query type is dispatched via <c>ISender</c>
    /// but no corresponding handler implementation is found in the compilation.
    /// </remarks>
    public static readonly DiagnosticDescriptor CallSiteNoHandler = new(
        id: "MO0020",
        title: "Command/query sent but no handler found",
        messageFormat: "Type '{0}' is sent via {1} but no handler was found in this assembly. Ensure a handler is registered.",
        category: "Mediator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
