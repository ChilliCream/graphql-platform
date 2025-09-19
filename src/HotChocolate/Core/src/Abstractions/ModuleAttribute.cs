namespace HotChocolate;

/// <summary>
/// Specifies the assembly module name that is being used in combination
/// with the HotChocolate.Types.Analyzers source generators.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class ModuleAttribute : Attribute
{
    /// <summary>
    /// Initializes new instance of <see cref="ModuleAttribute"/>.
    /// </summary>
    /// <param name="name">
    /// The module name.
    /// </param>
    /// <param name="options">
    /// The source generator features.
    /// </param>
    public ModuleAttribute(string name, ModuleOptions options = ModuleOptions.Default)
    {
        Name = name;
        Options = options;
    }

    /// <summary>
    /// Gets the module name.
    /// </summary>
    /// <value></value>
    public string Name { get; }

    /// <summary>
    /// Gets the selected source generator options.
    /// </summary>
    public ModuleOptions Options { get; }
}
