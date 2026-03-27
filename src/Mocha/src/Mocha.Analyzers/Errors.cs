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
}
