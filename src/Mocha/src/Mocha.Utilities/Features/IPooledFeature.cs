namespace Mocha.Features;

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
