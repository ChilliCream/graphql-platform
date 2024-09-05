namespace HotChocolate;

/// <summary>
/// Specifies that a resolver parameter represents the parent object.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ParentAttribute(string? requires = null) : Attribute
{
    /// <summary>
    /// Gets a string representing the property requirements for the parent object.
    /// </summary>
    public string? Requires { get; } = requires;
}
