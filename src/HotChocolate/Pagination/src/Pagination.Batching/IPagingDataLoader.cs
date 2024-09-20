using GreenDonut;

namespace HotChocolate.Pagination;

/// <summary>
/// A paging DataLoader is a version of a DataLoader that
/// branches per paging arguments.
/// </summary>
/// <typeparam name="TKey">
/// The type of the key.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the value.
/// </typeparam>
public interface IPagingDataLoader<in TKey, TValue>
    : IDataLoader<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    /// Gets the root DataLoader instance from which this instance was branched off.
    /// </summary>
    IDataLoader<TKey, TValue> Root { get; }

    /// <summary>
    /// Gets the paging arguments for this DataLoader.
    /// </summary>
    PagingArguments PagingArguments { get; }
}
