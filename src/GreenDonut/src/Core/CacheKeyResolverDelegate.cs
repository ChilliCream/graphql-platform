namespace GreenDonut
{
    /// <summary>
    /// A delegate which is responsible for transforming the key that is used
    /// for accessing the backend, into a cache key before accessing the cache
    /// in any way.
    /// </summary>
    /// <typeparam name="TKey">A key type.</typeparam>
    /// <param name="key">A key.</param>
    /// <returns>A cache key.</returns>
    public delegate object CacheKeyResolverDelegate<in TKey>(TKey key);
}
