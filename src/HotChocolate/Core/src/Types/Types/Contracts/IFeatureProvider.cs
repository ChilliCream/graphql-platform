#nullable enable
using HotChocolate.Features;

namespace HotChocolate.Types;

/// <summary>
/// GraphQL type system members that have features.
/// </summary>
public interface IFeatureProvider
{
    /// <summary>
    /// Gets the features of this type system member.
    /// </summary>
    public IFeatureCollection Features { get; }
}
