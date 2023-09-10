using System;

namespace HotChocolate;

/// <summary>
/// Marks a method as a DataLoader which will trigger the Hot Chocolate
/// types source generator to generate necessary code around this method.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class DataLoaderAttribute : Attribute
{
    public DataLoaderAttribute(string? name = null)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the name override for the DataLoader or <c>null</c>.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Specifies how services by default are handled.
    /// </summary>
    public DataLoaderServiceScope ServiceScope { get; set; }

    /// <summary>
    /// Specifies if the DataLoader is generated as public or private class.
    /// </summary>
    public DataLoaderAccessModifier AccessModifier { get; set; }
}
