namespace GreenDonut.Selectors;

/// <summary>
/// A selection DataLoader is a version of a DataLoader that
/// selects a different shape of data of the original DataLoader.
/// The data that is fetched with this DataLoader version is
/// not propagated to other DataLoader and is isolated within the
/// DataLoader instance. This allows to fetch the data in an optimized
/// way for specific uses cases.
/// </summary>
/// <typeparam name="TKey">
/// The type of the key.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the value.
/// </typeparam>
public interface ISelectionDataLoader<in TKey, TValue>
    : IDataLoader<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    /// Gets the root DataLoader instance from which this instance was branched off.
    /// </summary>
    IDataLoader<TKey, TValue> Root { get; }
}
