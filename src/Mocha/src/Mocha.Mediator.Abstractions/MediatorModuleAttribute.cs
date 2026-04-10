namespace Mocha.Mediator;

/// <summary>
/// Specifies the assembly module name that is being used in combination
/// with the Mocha.Analyzers source generators.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class MediatorModuleAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="MediatorModuleAttribute"/>.
    /// </summary>
    /// <param name="name">
    /// The module name.
    /// </param>
    public MediatorModuleAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <summary>
    /// Gets the module name.
    /// </summary>
    public string Name { get; }
}
