using System.Collections.Immutable;

namespace Mocha;

/// <summary>
/// A read-only registry of messaging conventions that supports efficient type-filtered retrieval.
/// </summary>
public interface IConventionRegistry : IReadOnlyList<IConvention>
{
    /// <summary>
    /// Returns all registered conventions that implement the specified convention type.
    /// </summary>
    /// <typeparam name="TConvention">The convention type to filter by.</typeparam>
    /// <returns>An immutable array of matching conventions.</returns>
    ImmutableArray<TConvention> GetConventions<TConvention>() where TConvention : IConvention;
}
