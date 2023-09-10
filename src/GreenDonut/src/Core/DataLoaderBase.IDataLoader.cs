using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GreenDonut;

public abstract partial class DataLoaderBase<TKey, TValue>
{
    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    void IDataLoader.Remove(object key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        Remove((TKey)key);
    }

    /// <inheritdoc />
    void IDataLoader.Set(object key, Task<object?> value)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        Set((TKey)key, AwaitValue());

        async Task<TValue> AwaitValue() => (TValue)(await value.ConfigureAwait(false))!;
    }

    /// <inheritdoc />
    public void Clear() => Cache?.Clear();
}
