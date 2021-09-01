namespace GreenDonut
{
    /// <summary>
    /// A delegate which is responsible for transforming the key that is used
    /// for accessing the backend, into a cache key before accessing the cache
    /// in any way.
    /// </summary>
    /// <param name="key">A key.</param>
    /// <param name="type">A type.</param>
    /// <returns>A cache key.</returns>
    public delegate TaskCacheKey CacheKeyFactoryDelegate(string type, object key);

    public delegate string CacheKeyTypeFactoryDelegate(IDataLoader dataLoader);
}
