namespace Mocha;

/// <summary>
/// Annotates a generated messaging registration method with metadata about the message types
/// it registers. This enables cross-project analyzers to discover types registered by
/// referenced modules without scanning assemblies.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class MessagingModuleInfoAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the message types registered by this module.
    /// </summary>
    public Type[] MessageTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the saga types registered by this module.
    /// </summary>
    public Type[] SagaTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the handler types registered by this module.
    /// </summary>
    public Type[] HandlerTypes { get; set; } = [];
}
