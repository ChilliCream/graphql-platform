using System.Diagnostics.CodeAnalysis;

namespace Mocha.Mediator;

/// <summary>
/// Holds the resolved configuration for a mediator handler, including its type metadata,
/// handler kind, and an optional pre-built terminal delegate.
/// </summary>
public class MediatorHandlerConfiguration : MediatorConfiguration
{
    /// <summary>
    /// Gets or sets the concrete handler implementation type.
    /// </summary>
    public Type? HandlerType { get; set; }

    /// <summary>
    /// Gets or sets the message type (command, query, or notification type).
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
    public Type? MessageType { get; set; }

    /// <summary>
    /// Gets or sets the response type, or null for void commands and notifications.
    /// </summary>
    public Type? ResponseType { get; set; }

    /// <summary>
    /// Gets or sets the kind of handler.
    /// </summary>
    public MediatorHandlerKind Kind { get; set; }

    /// <summary>
    /// Gets or sets an optional pre-built delegate. When set, the builder uses this
    /// directly (AOT-safe, source-generator path). When null, the builder creates the delegate
    /// via reflection (manual AddHandler path).
    /// This is the innermost delegate that resolves and invokes the handler.
    /// It is wrapped in middleware during pipeline compilation.
    /// </summary>
    public MediatorDelegate? Delegate { get; set; }
}
