using System.Collections.Immutable;

namespace HotChocolate.Types.Analyzers.Models;

/// <summary>
/// Contains documentation extracted from XML comments for a method.
/// </summary>
public readonly struct MethodDescription
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
