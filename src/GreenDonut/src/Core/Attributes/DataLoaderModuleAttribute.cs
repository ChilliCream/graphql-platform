namespace GreenDonut;

/// <summary>
/// Specifies the assembly module name that is being used in combination
/// with the HotChocolate.Types.Analyzers source generators.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class DataLoaderModuleAttribute : Attribute
{
    /// <summary>
    /// Initializes new instance of <see cref="DataLoaderModuleAttribute"/>.
    /// </summary>
    /// <param name="name">
    /// The module name.
    /// </param>
    public DataLoaderModuleAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the module name.
    /// </summary>
    /// <value></value>
    public string Name { get; }
}
