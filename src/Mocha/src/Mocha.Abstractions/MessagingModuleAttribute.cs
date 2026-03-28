namespace Mocha;

/// <summary>
/// Specifies the assembly module name that is being used in combination
/// with the Mocha.Analyzers source generators for MessageBus handler registration.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class MessagingModuleAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="MessagingModuleAttribute"/>.
    /// </summary>
    /// <param name="name">
    /// The module name.
    /// </param>
    public MessagingModuleAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <summary>
    /// Gets the module name.
    /// </summary>
    public string Name { get; }
}
