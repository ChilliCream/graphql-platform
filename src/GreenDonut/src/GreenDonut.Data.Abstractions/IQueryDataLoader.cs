namespace GreenDonut.Data;

/// <summary>
/// A query DataLoader is a specialized version of a DataLoader that has query context which
/// allows to manipulate the database request.
/// </summary>
/// <typeparam name="TKey">
/// The type of the key.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the value.
/// </typeparam>
public interface IQueryDataLoader<in TKey, TValue>
    : IDataLoader<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    /// Gets the root DataLoader instance from which this instance was derived.
    /// </summary>
    IDataLoader<TKey, TValue> Root { get; }
}
