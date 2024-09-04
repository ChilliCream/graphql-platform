namespace HotChocolate;

/// <summary>
/// Specifies that a resolver parameter represents the parent object.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ParentAttribute(string? properties = null) : Attribute
{
    /// <summary>
    /// Gets a string representing the property requirements for the parent object.
    /// </summary>
    public string? Properties { get; } = properties;
}
