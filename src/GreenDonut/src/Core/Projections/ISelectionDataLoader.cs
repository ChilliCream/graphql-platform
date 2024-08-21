#if NET8_0_OR_GREATER
using System.Collections.Immutable;

namespace GreenDonut.Projections;

public interface ISelectionDataLoader<in TKey, TValue>
    : IDataLoader<TKey, TValue> where TKey : notnull
{
    IDataLoader<TKey, TValue> Root { get; }
}

internal sealed class SelectionDataLoader<TKey, TValue>
    : ISelectionDataLoader<TKey, TValue> where TKey : notnull
{
    private readonly DataLoaderBase<TKey, TValue> _root;
    private readonly string _cacheKeyType;

    public SelectionDataLoader(DataLoaderBase<TKey, TValue> root, string key)
    {
        _root = root;
        ContextData = root.ContextData;
        _cacheKeyType = $"{root.CacheKeyType}.{key}";
    }

    public IDataLoader<TKey, TValue> Root => _root;

    public IImmutableDictionary<string, object?> ContextData { get; set; }

    public Task<TValue> LoadAsync(
        TKey key,
        CancellationToken cancellationToken = default)
        => _root.LoadAsync(key, _cacheKeyType, true, cancellationToken);

    public Task<IReadOnlyList<TValue>> LoadAsync(
        IReadOnlyCollection<TKey> keys,
        CancellationToken cancellationToken = default)
        => _root.LoadAsync(keys, _cacheKeyType, true, cancellationToken);

    Task<object?> IDataLoader.LoadAsync(
        object key,
        CancellationToken cancellationToken)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return Load();

        async Task<object?> Load()
            => await LoadAsync((TKey)key, cancellationToken).ConfigureAwait(false);
    }

    Task<IReadOnlyList<object?>> IDataLoader.LoadAsync(
        IReadOnlyCollection<object> keys,
        CancellationToken cancellationToken)
    {
        if (keys is null)
        {
            throw new ArgumentNullException(nameof(keys));
        }

        return Load();

        async Task<IReadOnlyList<object?>> Load()
        {
            var casted = keys.Select(key => (TKey)key).ToArray();
            return (IReadOnlyList<object?>)
                await LoadAsync(casted, cancellationToken).ConfigureAwait(false);
        }
    }

    public void Set(TKey key, Task<TValue> value)
        => throw new NotSupportedException();

    public void Set(object key, Task<object?> value)
        => throw new NotSupportedException();

    public void Remove(TKey key)
        => throw new NotSupportedException();

    public void Remove(object key)
        => throw new NotSupportedException();

    public void Clear()
        => throw new NotSupportedException();

    public ISelectionDataLoader<TKey, TValue> Branch(string key)
        => throw new NotSupportedException();
}
#endif
