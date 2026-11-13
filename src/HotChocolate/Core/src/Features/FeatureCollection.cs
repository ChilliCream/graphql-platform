// This code was originally forked of https://github.com/dotnet/aspnetcore/tree/c7aae8ff34dce81132d0fb3a976349dcc01ff903/src/Extensions/Features/src

// ReSharper disable NonAtomicCompoundOperator
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace HotChocolate.Features;

/// <summary>
/// Default implementation for <see cref="IFeatureCollection"/>.
/// </summary>
public sealed class FeatureCollection : IFeatureCollection
{
    private const int SmallStoreSize = 4;
    private static readonly KeyComparer s_featureKeyComparer = new();
    private readonly IFeatureCollection? _defaults;
    private readonly int _initialCapacity;

    // The store is null until the first feature is set. Up to SmallStoreSize features are kept in a
    // KeyValuePair<Type, object>[] where free slots have a null key, more features in a Dictionary<Type, object>.
    private object? _store;
    private volatile int _containerRevision;

    /// <summary>
    /// Initializes a new instance of <see cref="FeatureCollection"/>.
    /// </summary>
    public FeatureCollection()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FeatureCollection"/> with the specified initial capacity.
    /// </summary>
    /// <param name="initialCapacity">
    /// The initial number of elements that the collection can contain.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="initialCapacity"/> is less than 0
    /// </exception>
    public FeatureCollection(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(initialCapacity);

        _initialCapacity = initialCapacity;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FeatureCollection"/> with the specified defaults.
    /// </summary>
    /// <param name="defaults">
    /// The feature defaults.
    /// </param>
    public FeatureCollection(IFeatureCollection defaults)
    {
        _defaults = defaults;
    }

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public bool IsEmpty
    {
        get
        {
            var store = _store;

            if (store is KeyValuePair<Type, object>[] items)
            {
                if (CountItems(items) > 0)
                {
                    return false;
                }
            }
            else if (store is not null && Unsafe.As<Dictionary<Type, object>>(store).Count > 0)
            {
                return false;
            }

            return _defaults?.IsEmpty ?? true;
        }
    }

    /// <inheritdoc />
    public int Revision => _containerRevision + (_defaults?.Revision ?? 0);

    /// <inheritdoc />
    public object? this[Type key]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(key);

            return TryGetOwnFeature(key, out var result) ? result : _defaults?[key];
        }
        set
        {
            ArgumentNullException.ThrowIfNull(key);

            if (value == null)
            {
                if (RemoveOwnFeature(key))
                {
                    _containerRevision++;
                }
                return;
            }

            SetOwnFeature(key, value);
            _containerRevision++;
        }
    }

    /// <inheritdoc />
    public TFeature? Get<TFeature>()
    {
        if (typeof(TFeature).IsValueType)
        {
            var feature = this[typeof(TFeature)];
            if (feature is null && Nullable.GetUnderlyingType(typeof(TFeature)) is null)
            {
                throw new InvalidOperationException(
                    $"{typeof(TFeature).FullName} does not exist in the feature collection "
                    + "and because it is a struct the method can't return null. "
                    + $"Use 'featureCollection[typeof({typeof(TFeature).FullName})] is not null' "
                    + "to check if the feature exists.");
            }
            return (TFeature?)feature;
        }

        return (TFeature?)this[typeof(TFeature)];
    }

    /// <inheritdoc />
    public bool TryGet<TFeature>([NotNullWhen(true)] out TFeature? feature)
    {
        if (TryGetOwnFeature(typeof(TFeature), out var result))
        {
            if (result is TFeature f)
            {
                feature = f;
                return true;
            }

            feature = default;
            return false;
        }

        if (_defaults is not null && _defaults.TryGet(out feature))
        {
            return true;
        }

        feature = default;
        return false;
    }

    /// <inheritdoc />
    public void Set<TFeature>(TFeature? instance)
    {
        this[typeof(TFeature)] = instance;
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
    {
        var store = _store;

        if (store is KeyValuePair<Type, object>[] items)
        {
            foreach (var pair in items)
            {
                if (pair.Key is not null)
                {
                    yield return pair;
                }
            }
        }
        else if (store is not null)
        {
            foreach (var pair in Unsafe.As<Dictionary<Type, object>>(store))
            {
                yield return pair;
            }
        }

        if (_defaults != null)
        {
            // Don't return features masked by the wrapper.
            var features = store is null
                ? _defaults
                : _defaults.Except(GetOwnFeatures(store), s_featureKeyComparer);

            foreach (var pair in features)
            {
                yield return pair;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Copies the features of this collection into an array that has exactly one element per feature.
    /// </summary>
    /// <param name="maxCount">
    /// The maximum number of features to copy.
    /// </param>
    /// <param name="features">
    /// The copied features, or <c>null</c> if nothing was copied.
    /// </param>
    /// <returns>
    /// <c>true</c> if the features were copied; <c>false</c> if this collection has defaults
    /// or contains more than <paramref name="maxCount"/> features.
    /// </returns>
    internal bool TryCopyFeatures(int maxCount, [NotNullWhen(true)] out KeyValuePair<Type, object>[]? features)
    {
        var store = _store;

        if (_defaults is not null)
        {
            features = null;
            return false;
        }

        if (store is null)
        {
            features = [];
            return true;
        }

        if (store is KeyValuePair<Type, object>[] items)
        {
            if (CountItems(items) > maxCount)
            {
                features = null;
                return false;
            }

            features = CopyItems(items);
            return true;
        }

        var dictionary = Unsafe.As<Dictionary<Type, object>>(store);

        if (dictionary.Count > maxCount)
        {
            features = null;
            return false;
        }

        features = new KeyValuePair<Type, object>[dictionary.Count];
        var index = 0;

        foreach (var pair in dictionary)
        {
            features[index++] = pair;
        }

        return true;
    }

    private bool TryGetOwnFeature(Type key, [NotNullWhen(true)] out object? value)
    {
        var store = _store;

        if (store is KeyValuePair<Type, object>[] items)
        {
            for (var i = 0; i < items.Length; i++)
            {
                if (items[i].Key == key)
                {
                    value = items[i].Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        if (store is not null)
        {
            return Unsafe.As<Dictionary<Type, object>>(store).TryGetValue(key, out value);
        }

        value = null;
        return false;
    }

    private void SetOwnFeature(Type key, object value)
    {
        var store = _store;

        if (store is KeyValuePair<Type, object>[] items)
        {
            var used = 0;
            var count = 0;

            for (var i = 0; i < items.Length; i++)
            {
                var itemKey = items[i].Key;

                if (itemKey is null)
                {
                    continue;
                }

                if (itemKey == key)
                {
                    items[i] = new KeyValuePair<Type, object>(itemKey, value);
                    return;
                }

                used = i + 1;
                count++;
            }

            if (used < items.Length)
            {
                items[used] = new KeyValuePair<Type, object>(key, value);
                return;
            }

            // Compact into a new store instead of moving features between the slots of the current one.
            if (count < items.Length)
            {
                var compacted = new KeyValuePair<Type, object>[SmallStoreSize];
                CopyItems(items, compacted);
                compacted[count] = new KeyValuePair<Type, object>(key, value);
                _store = compacted;
                return;
            }

            var features = new Dictionary<Type, object>(SmallStoreSize * 2);

            foreach (var pair in items)
            {
                features[pair.Key] = pair.Value;
            }

            features[key] = value;
            _store = features;
            return;
        }

        if (store is not null)
        {
            Unsafe.As<Dictionary<Type, object>>(store)[key] = value;
            return;
        }

        if (_initialCapacity > SmallStoreSize)
        {
            _store = new Dictionary<Type, object>(_initialCapacity) { [key] = value };
            return;
        }

        var newItems = new KeyValuePair<Type, object>[SmallStoreSize];
        newItems[0] = new KeyValuePair<Type, object>(key, value);
        _store = newItems;
    }

    private bool RemoveOwnFeature(Type key)
    {
        var store = _store;

        if (store is KeyValuePair<Type, object>[] items)
        {
            for (var i = 0; i < items.Length; i++)
            {
                if (items[i].Key == key)
                {
                    items[i] = default;
                    return true;
                }
            }

            return false;
        }

        return store is not null && Unsafe.As<Dictionary<Type, object>>(store).Remove(key);
    }

    private static IEnumerable<KeyValuePair<Type, object>> GetOwnFeatures(object store)
        => store is KeyValuePair<Type, object>[] items
            ? CopyItems(items)
            : Unsafe.As<Dictionary<Type, object>>(store);

    private static int CountItems(KeyValuePair<Type, object>[] items)
    {
        var count = 0;

        foreach (var pair in items)
        {
            if (pair.Key is not null)
            {
                count++;
            }
        }

        return count;
    }

    private static KeyValuePair<Type, object>[] CopyItems(KeyValuePair<Type, object>[] items)
    {
        var count = CountItems(items);

        if (count == 0)
        {
            return [];
        }

        var copy = new KeyValuePair<Type, object>[count];
        CopyItems(items, copy);
        return copy;
    }

    private static void CopyItems(KeyValuePair<Type, object>[] items, KeyValuePair<Type, object>[] destination)
    {
        var index = 0;

        foreach (var pair in items)
        {
            if (pair.Key is not null)
            {
                destination[index++] = pair;
            }
        }
    }

    private sealed class KeyComparer : IEqualityComparer<KeyValuePair<Type, object>>
    {
        public bool Equals(KeyValuePair<Type, object> x, KeyValuePair<Type, object> y)
            => x.Key.Equals(y.Key);

        public int GetHashCode(KeyValuePair<Type, object> obj) =>
            obj.Key.GetHashCode();
    }

    /// <summary>
    /// Gets an empty feature collection.
    /// </summary>
    public static IFeatureCollection Empty => EmptyFeatureCollection.Default;
}
