namespace GreenDonut;

/// <summary>
/// Identifies a data loader that caches individually fetched values.
/// </summary>
/// <typeparam name="TKey">A key type.</typeparam>
/// <typeparam name="TValue">A value type.</typeparam>
public interface ICacheDataLoader<in TKey, TValue>
    : IDataLoader<TKey, TValue>
    where TKey : notnull;
