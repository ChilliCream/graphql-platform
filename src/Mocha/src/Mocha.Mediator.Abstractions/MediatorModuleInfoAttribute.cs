namespace Mocha.Mediator;

/// <summary>
/// Annotates a generated mediator registration method with metadata about the message types
/// it registers. This enables cross-project analyzers to discover types registered by
/// referenced modules without scanning assemblies.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class MediatorModuleInfoAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the message types registered by this module.
    /// </summary>
    public Type[] MessageTypes { get; set; } = [];
}
