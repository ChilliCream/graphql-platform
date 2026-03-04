using System.Collections.Immutable;

namespace HotChocolate.Types.Analyzers.Models;

/// <summary>
/// Contains documentation extracted from XML comments for a method.
/// </summary>
public readonly struct MethodDescription : IMemberDescription
{
    /// <summary>
    /// Initializes a new instance of <see cref="MethodDescription"/>.
    /// </summary>
    /// <param name="description">The method's summary documentation.</param>
    /// <param name="parameterDescriptions">
    /// The parameter descriptions in the same order as the method parameters.
    /// </param>
    public MethodDescription(
        string? description,
        ImmutableArray<string?> parameterDescriptions)
    {
        Description = description;
        ParameterDescriptions = parameterDescriptions;
    }

    /// <summary>
    /// Gets the method's summary documentation, or <c>null</c> if not documented.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the parameter descriptions in the same order as the method parameters.
    /// Each element is <c>null</c> if the parameter is not documented.
    /// </summary>
    public ImmutableArray<string?> ParameterDescriptions { get; }
}

public readonly record struct PropertyDescription(string? Description) : IMemberDescription
{
    public string? Description { get; } = Description;
}

public readonly record struct ParameterDescription(string? Description) : IMemberDescription
{
    public string? Description { get; } = Description;
}

public interface IMemberDescription
{
    string? Description { get; }
}
