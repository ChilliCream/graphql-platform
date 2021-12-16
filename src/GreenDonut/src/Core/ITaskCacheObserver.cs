namespace GreenDonut;

/// <summary>
/// A <see cref="IDataLoader{TKey,TValue}"/> that implements this interface, will automatically
/// be subscribed to the cache. This dataloader will then receive updates from the cache.
/// </summary>
public interface ITaskCacheObserver
{
    /// <summary>
    /// Checks if the received object from the cache can be handled by this observer. If this
    /// method returns true, then the value is passed to <see cref="OnNext"/>.
    /// </summary>
    /// <param name="value">The value received from the cache</param>
    /// <returns>if <c>true</c> then the cache can process the value</returns>
    bool CanHandle(object value);

    /// <summary>
    /// Is called by the observer from the cache. This method receives all values from the cache
    /// that have passed <see cref="CanHandle"/>.
    /// Each value is only received once and values that are published from this dataloader,
    /// will not be observed
    /// </summary>
    /// <param name="value"></param>
    void OnNext(object value);
}
