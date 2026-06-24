namespace Mocha;

/// <summary>
/// Specifies the binding mode for a messaging transport or endpoint, controlling whether
/// convention-based discovery and bindings are applied automatically or require explicit configuration.
/// </summary>
public enum MessagingBindMode
{
    /// <summary>
    /// Convention-based discovery and bindings are applied automatically.
    /// Consumers are discovered and matched to endpoints by naming conventions,
    /// and convention binds (exchange-to-queue, topic-to-queue) are generated for consumed message types.
    /// This is the default mode.
    /// </summary>
    Implicit,

    /// <summary>
    /// Discovery and convention binds are suppressed.
    /// Consumers must be explicitly placed on endpoints, and bindings must be declared manually
    /// or overridden at the queue scope.
    /// </summary>
    Explicit
}
