#nullable disable
using System.Collections.Immutable;

namespace HotChocolate.Internal;

/// <summary>
/// Represents metadata about a parameter used for binding resolution.
/// This descriptor provides compile-time parameter information without requiring reflection.
/// </summary>
public readonly struct ParameterDescriptor
{
    /// <summary>
    /// Initializes a new instance of <see cref="ParameterDescriptor"/>.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="type">The type of the parameter.</param>
    /// <param name="isNullable">Defines if the <paramref name="type"/> is nullable</param>
    /// <param name="attributes">The attributes applied to the parameter.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name"/> or <paramref name="type"/> is <c>null</c>.
    /// </exception>
    public ParameterDescriptor(
        string name,
        Type type,
        bool isNullable,
        ImmutableArray<object> attributes)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(type);

        Name = name;
        Type = type;
        IsNullable = isNullable;
        Attributes = attributes;
    }

    /// <summary>
    /// Gets the name of the parameter.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the type of the parameter.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Defines if the <see cref="Type"/> is nullable.
    /// </summary>
    public bool IsNullable { get; }

    /// <summary>
    /// Gets the attributes applied to the parameter.
    /// </summary>
    public ImmutableArray<object> Attributes { get; }
}
