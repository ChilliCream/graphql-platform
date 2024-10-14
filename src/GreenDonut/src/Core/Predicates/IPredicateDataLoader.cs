namespace GreenDonut.Predicates;

/// <summary>
/// A predicate DataLoader is a specialized version of a DataLoader that
/// selects a subset of data based on a given predicate from the original DataLoader.
/// The data retrieved by this DataLoader is not shared with other DataLoaders and
/// remains isolated within this instance.
/// </summary>
/// <typeparam name="TKey">
/// The type of the key.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the value.
/// </typeparam>
public interface IPredicateDataLoader<in TKey, TValue>
    : IDataLoader<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    /// Gets the root DataLoader instance from which this instance was derived.
    /// </summary>
    IDataLoader<TKey, TValue> Root { get; }
}
