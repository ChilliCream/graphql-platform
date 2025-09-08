namespace GreenDonut;

/// <summary>
/// Marks a method as a DataLoader which will trigger the Hot Chocolate
/// types source generator to generate necessary code around this method.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class DataLoaderAttribute(string? name = null) : Attribute
{
    /// <summary>
    /// Gets the name override for the DataLoader or <c>null</c>.
    /// </summary>
    public string? Name { get; } = name;

    /// <summary>
    /// Gets or sets additional lookup methods that are used
    /// to create additional cache lookups for entities.
    /// </summary>
    public string[] Lookups { get; set; } = [];

    /// <summary>
    /// Specifies how services by default are handled.
    /// </summary>
    public DataLoaderServiceScope ServiceScope { get; set; }

    /// <summary>
    /// Specifies if the DataLoader is generated as public or private class.
    /// </summary>
    public DataLoaderAccessModifier AccessModifier { get; set; }
}
