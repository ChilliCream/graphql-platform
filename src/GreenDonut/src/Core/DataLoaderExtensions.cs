using System.Buffers;
using System.Collections.Immutable;
using System.Text;

namespace GreenDonut;

/// <summary>
/// A bunch of convenient <c>DataLoader</c> extension methods.
/// </summary>
public static class DataLoaderExtensions
{
    /// <summary>
    /// Loads a single value by key. This call may return a cached value
    /// or enqueues this single request for batching if enabled.
    /// </summary>
    /// <param name="dataLoader">
    /// A data loader instance.
    /// </param>
    /// <param name="key">A unique key.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="key"/> is <c>null</c>.
    /// </exception>
    /// <returns>
    /// A single result which may contain a value or information about the
    /// error which may occurred during the call.
    /// </returns>
    public static async Task<TValue> LoadRequiredAsync<TKey, TValue>(
        this IDataLoader<TKey, TValue> dataLoader,
        TKey key,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        where TValue : notnull
    {
        if (dataLoader == null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        var value = await dataLoader.LoadAsync(key, cancellationToken).ConfigureAwait(false);

        if (value is null)
        {
            throw new KeyNotFoundException($"The key {key} could not be resolved.");
        }

        return value;
    }

    /// <summary>
    /// Loads multiple values by keys. This call may return cached values
    /// and enqueues requests which were not cached for batching if
    /// enabled.
    /// </summary>
    /// <param name="dataLoader">
    /// A data loader instance.
    /// </param>
    /// <param name="keys">A list of unique keys.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="keys"/> is <c>null</c>.
    /// </exception>
    /// <returns>
    /// A list of values in the same order as the provided keys.
    /// </returns>
    public static async Task<IReadOnlyList<TValue>> LoadRequiredAsync<TKey, TValue>(
        this IDataLoader<TKey, TValue> dataLoader,
        IReadOnlyCollection<TKey> keys,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        where TValue : notnull
    {
        if (dataLoader == null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        if (keys == null)
        {
            throw new ArgumentNullException(nameof(keys));
        }

        var values = await dataLoader.LoadAsync(keys, cancellationToken).ConfigureAwait(false);

        if(values.Any(t => t is null))
        {
            throw new KeyNotFoundException(CreateMissingKeyValueMessage(keys, values));
        }

        return values!;
    }

    private static string CreateMissingKeyValueMessage<TKey, TValue>(
        IReadOnlyCollection<TKey> keys,
        IReadOnlyList<TValue> values)
    {
        var buffer = new StringBuilder();

        var i = 0;
        var first = true;
        var multipleMissing = false;

        foreach (var key in keys)
        {
            if (values[i] == null)
            {
                if(!first)
                {
                    multipleMissing = true;
                    buffer.Append(", ");
                }

                buffer.Append(key);
                first = false;
            }

            i++;
        }

        if (multipleMissing)
        {
            buffer.Insert(0, "The keys `");
        }
        else
        {
            buffer.Insert(0, "The key `");
        }

        buffer.Append("` could not be resolved.");

        return buffer.ToString();
    }

    /// <summary>
    /// Adds a new entry to the cache if not already exists.
    /// </summary>
    /// <typeparam name="TKey">A key type.</typeparam>
    /// <typeparam name="TValue">A value type.</typeparam>
    /// <param name="dataLoader">A data loader instance.</param>
    /// <param name="key">A cache entry key.</param>
    /// <param name="value">A cache entry value.</param>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="key"/> is <c>null</c>.
    /// </exception>
    public static void SetCacheEntry<TKey, TValue>(
        this IDataLoader<TKey, TValue> dataLoader,
        TKey key,
        TValue? value)
        where TKey : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        dataLoader.SetCacheEntry(key, Task.FromResult(value));
    }

    /// <summary>
    /// Adds a new entry to the cache if not already exists.
    /// </summary>
    /// <param name="dataLoader">A data loader instance.</param>
    /// <param name="key">A cache entry key.</param>
    /// <param name="value">A cache entry value.</param>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="key"/> is <c>null</c>.
    /// </exception>
    public static void SetCacheEntry(
        this IDataLoader dataLoader,
        object key,
        object? value)
    {
        if (dataLoader == null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        dataLoader.SetCacheEntry(key, Task.FromResult(value));
    }

    /// <summary>
    /// Adds a new entry to the cache if not already exists.
    /// </summary>
    /// <typeparam name="TKey">A key type.</typeparam>
    /// <typeparam name="TValue">A value type.</typeparam>
    /// <param name="dataLoader">A data loader instance.</param>
    /// <param name="key">A cache entry key.</param>
    /// <param name="value">A cache entry value.</param>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="key"/> is <c>null</c>.
    /// </exception>
    [Obsolete("Use SetCacheEntry instead.")]
    public static void Set<TKey, TValue>(
        this IDataLoader<TKey, TValue> dataLoader,
        TKey key,
        TValue? value)
        where TKey : notnull
    {
        SetCacheEntry(dataLoader, key, value);
    }

    /// <summary>
    /// Adds a new entry to the cache if not already exists.
    /// </summary>
    /// <param name="dataLoader">A data loader instance.</param>
    /// <param name="key">A cache entry key.</param>
    /// <param name="value">A cache entry value.</param>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="key"/> is <c>null</c>.
    /// </exception>
    [Obsolete("Use SetCacheEntry instead.")]
    public static void Set(
        this IDataLoader dataLoader,
        object key,
        object? value)
    {
        SetCacheEntry(dataLoader, key, value);
    }

    /// <summary>
    /// Sets a state value on the data loader.
    /// </summary>
    /// <param name="dataLoader">
    /// The data loader instance.
    /// </param>
    /// <param name="value">
    /// The state value.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type of the DataLoader.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type of the DataLoader.
    /// </typeparam>
    /// <typeparam name="TState">
    /// The state type.
    /// </typeparam>
    /// <returns>
    /// Returns the data loader instance.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    public static IDataLoader<TKey, TValue> SetState<TKey, TValue, TState>(
        this IDataLoader<TKey, TValue> dataLoader,
        TState value)
        where TKey : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        return dataLoader.SetState(typeof(TState).FullName ?? typeof(TState).Name, value);
    }

    /// <summary>
    /// Sets a state value on the data loader.
    /// </summary>
    /// <param name="dataLoader">
    /// The data loader instance.
    /// </param>
    /// <param name="key">
    /// The state key.
    /// </param>
    /// <param name="value">
    /// The state value.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type of the DataLoader.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type of the DataLoader.
    /// </typeparam>
    /// <typeparam name="TState">
    /// The state type.
    /// </typeparam>
    /// <returns>
    /// Returns the data loader instance.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Throws if <paramref name="key"/> is <c>null</c> or empty.
    /// </exception>
    public static IDataLoader<TKey, TValue> SetState<TKey, TValue, TState>(
        this IDataLoader<TKey, TValue> dataLoader,
        string key,
        TState value)
        where TKey : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException(
                "The key must not be null or empty.",
                nameof(key));
        }

        dataLoader.ContextData = dataLoader.ContextData.SetItem(key, value);
        return dataLoader;
    }

    /// <summary>
    /// Tries to set state value on the data loader if it not already exists.
    /// </summary>
    /// <param name="dataLoader">
    /// The data loader instance.
    /// </param>
    /// <param name="value">
    /// The state value.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type of the DataLoader.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type of the DataLoader.
    /// </typeparam>
    /// <typeparam name="TState">
    /// The state type.
    /// </typeparam>
    /// <returns>
    /// Returns the data loader instance.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    public static IDataLoader<TKey, TValue> TrySetState<TKey, TValue, TState>(
        this IDataLoader<TKey, TValue> dataLoader,
        TState value)
        where TKey : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        return dataLoader.TrySetState(typeof(TState).FullName ?? typeof(TState).Name, value);
    }

    /// <summary>
    /// Tries to set state value on the data loader if it not already exists.
    /// </summary>
    /// <param name="dataLoader">
    /// The data loader instance.
    /// </param>
    /// <param name="key">
    /// The state key.
    /// </param>
    /// <param name="value">
    /// The state value.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type of the DataLoader.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type of the DataLoader.
    /// </typeparam>
    /// <typeparam name="TState">
    /// The state type.
    /// </typeparam>
    /// <returns>
    /// Returns the data loader instance.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Throws if <paramref name="key"/> is <c>null</c> or empty.
    /// </exception>
    public static IDataLoader<TKey, TValue> TrySetState<TKey, TValue, TState>(
        this IDataLoader<TKey, TValue> dataLoader,
        string key,
        TState value)
        where TKey : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException(
                "The key must not be null or empty.",
                nameof(key));
        }

        if (!dataLoader.ContextData.ContainsKey(key))
        {
            dataLoader.ContextData = dataLoader.ContextData.SetItem(key, value);
        }

        return dataLoader;
    }

    /// <summary>
    /// Gets a state value from the <paramref name="dataLoader"/> or
    /// creates a new one and stores it as state on the <paramref name="dataLoader"/>.
    /// </summary>
    /// <param name="dataLoader">
    /// The data loader instance.
    /// </param>
    /// <param name="createValue">
    /// A factory that creates the new state value.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type of the DataLoader.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type of the DataLoader.
    /// </typeparam>
    /// <typeparam name="TState">
    /// The state type.
    /// </typeparam>
    /// <returns>
    /// Returns the state value.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    public static TState GetOrSetState<TKey, TValue, TState>(
        this IDataLoader<TKey, TValue> dataLoader,
        Func<string, TState> createValue)
        where TKey : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        var key = typeof(TState).FullName ?? typeof(TState).Name;

        if(!dataLoader.ContextData.TryGetValue(key, out var internalValue))
        {
            internalValue = createValue(key);
            dataLoader.ContextData = dataLoader.ContextData.SetItem(key, internalValue);
        }

        return (TState)internalValue!;
    }

    /// <summary>
    /// Gets a state value from the <paramref name="dataLoader"/> or
    /// creates a new one and stores it as state on the <paramref name="dataLoader"/>.
    /// </summary>
    /// <param name="dataLoader">
    /// The data loader instance.
    /// </param>
    /// <param name="key">
    /// The state key.
    /// </param>
    /// <param name="createValue">
    /// A factory that creates the new state value.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type of the DataLoader.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type of the DataLoader.
    /// </typeparam>
    /// <typeparam name="TState">
    /// The state type.
    /// </typeparam>
    /// <returns>
    /// Returns the state value.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Throws if <paramref name="key"/> is <c>null</c> or empty.
    /// </exception>
    public static TState GetOrSetState<TKey, TValue, TState>(
        this IDataLoader<TKey, TValue> dataLoader,
        string key,
        Func<string, TState> createValue)
        where TKey : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException(
                "The key must not be null or empty.",
                nameof(key));
        }

        if(!dataLoader.ContextData.TryGetValue(key, out var internalValue))
        {
            internalValue = createValue(key);
            dataLoader.ContextData = dataLoader.ContextData.SetItem(key, internalValue);
        }

        return (TState)internalValue!;
    }

    /// <summary>
    /// Adds the value to a collection that is stored on the DataLoader state.
    /// </summary>
    /// <param name="dataLoader">
    /// The data loader instance.
    /// </param>
    /// <param name="value">
    /// The state value.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type of the DataLoader.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type of the DataLoader.
    /// </typeparam>
    /// <typeparam name="TState">
    /// The state type.
    /// </typeparam>
    /// <returns>
    /// Returns the data loader instance.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    public static IDataLoader<TKey, TValue> AddStateEnumerable<TKey, TValue, TState>(
        this IDataLoader<TKey, TValue> dataLoader,
        TState value)
        where TKey : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        return dataLoader.AddStateEnumerable(typeof(TState).FullName ?? typeof(TState).Name, value);
    }

    /// <summary>
    /// Adds the value to a collection that is stored on the DataLoader state.
    /// </summary>
    /// <param name="dataLoader">
    /// The data loader instance.
    /// </param>
    /// <param name="key">
    /// The state key.
    /// </param>
    /// <param name="value">
    /// The state value.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type of the DataLoader.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type of the DataLoader.
    /// </typeparam>
    /// <typeparam name="TState">
    /// The state type.
    /// </typeparam>
    /// <returns>
    /// Returns the data loader instance.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Throws if <paramref name="key"/> is <c>null</c> or empty.
    /// </exception>
    public static IDataLoader<TKey, TValue> AddStateEnumerable<TKey, TValue, TState>(
        this IDataLoader<TKey, TValue> dataLoader,
        string key,
        TState value)
        where TKey : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException(
                "The key must not be null or empty.",
                nameof(key));
        }

        if (dataLoader.ContextData.TryGetValue(key, out var internalValue)
            && internalValue is ImmutableArray<TState> values)
        {
            dataLoader.ContextData = dataLoader.ContextData.SetItem(key, values.Add(value));
        }
        else
        {
            dataLoader.ContextData = dataLoader.ContextData.SetItem(key, ImmutableArray.Create(value));
        }

        return dataLoader;
    }
}
