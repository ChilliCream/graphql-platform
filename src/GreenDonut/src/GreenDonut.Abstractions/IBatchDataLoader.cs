namespace GreenDonut;

/// <summary>
/// Identifies a data loader that fetches multiple keys in a single batch.
/// </summary>
/// <typeparam name="TKey">A key type.</typeparam>
/// <typeparam name="TValue">A value type.</typeparam>
public interface IBatchDataLoader<in TKey, TValue>
    : IDataLoader<TKey, TValue>
    where TKey : notnull;
