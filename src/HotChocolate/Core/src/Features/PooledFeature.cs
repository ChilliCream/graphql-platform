// This code was originally forked of https://github.com/dotnet/aspnetcore/tree/c7aae8ff34dce81132d0fb3a976349dcc01ff903/src/Extensions/Features/src

// ReSharper disable NonAtomicCompoundOperator
namespace HotChocolate.Features;

/// <summary>
/// A feature that is pooled with a <see cref="PooledFeatureCollection"/>.
/// </summary>
public interface IPooledFeature
{
    /// <summary>
    /// Initializes the feature when the <see cref="PooledFeatureCollection"/> is rented out.
    /// </summary>
    /// <param name="state">
    /// The state of the <see cref="PooledFeatureCollection"/> that is being rented out.
    /// </param>
    void Initialize(object state);

    /// <summary>
    /// Resets the feature when the <see cref="PooledFeatureCollection"/> is returned to the pool.
    /// </summary>
    void Reset();
}