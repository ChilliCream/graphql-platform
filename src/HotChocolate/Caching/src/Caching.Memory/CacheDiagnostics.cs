namespace HotChocolate.Caching.Memory;

/// <summary>
/// The diagnostics for the cache.
/// </summary>
public abstract class CacheDiagnostics
{
    /// <summary>
    /// Registers a size gauge for the cache.
    /// </summary>
    /// <param name="sizeProvider">
    /// The function that provides the size of the cache.
    /// </param>
    public abstract void RegisterSizeGauge(Func<long> sizeProvider);

    /// <summary>
    /// Registers a capacity gauge for the cache.
    /// </summary>
    /// <param name="sizeProvider">
    /// The function that provides the capacity of the cache.
    /// </param>
    public abstract void RegisterCapacityGauge(Func<long> sizeProvider);

    /// <summary>
    /// Increments the hit counter.
    /// </summary>
    public abstract void Hit();

    /// <summary>
    /// Increments the miss counter.
    /// </summary>
    public abstract void Miss();

    /// <summary>
    /// Increments the eviction counter.
    /// </summary>
    public abstract void Evict();
}
