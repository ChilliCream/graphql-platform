using System;

namespace HotChocolate;

/// <summary>
/// Specifies resolver parameter represents a GraphQL field argument.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ArgumentAttribute : Attribute
{
    /// <summary>
    /// Specifies resolver parameter represents a GraphQL field argument.
    /// </summary>
    /// <param name="name">
    /// The name override for the GraphQL field argument.
    /// </param>
    public ArgumentAttribute(string? name = null)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the name override for the GraphQL field argument.
    /// </summary>
    public string? Name { get; }
}
