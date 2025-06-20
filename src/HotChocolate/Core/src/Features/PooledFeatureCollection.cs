// This code was originally forked of https://github.com/dotnet/aspnetcore/tree/c7aae8ff34dce81132d0fb3a976349dcc01ff903/src/Extensions/Features/src

// ReSharper disable NonAtomicCompoundOperator
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace HotChocolate.Features;

/// <summary>
/// A feature collection that is optimized for pooling.
/// </summary>
public sealed class PooledFeatureCollection : IFeatureCollection
{
    private static readonly KeyComparer s_featureKeyComparer = new();
    private readonly Dictionary<Type, object> _features = [];
    private readonly List<KeyValuePair<Type, object>> _pooledFeatures = [];
    private object _state;
    private IFeatureCollection? _defaults;
    private volatile int _containerRevision;

    /// <summary>
    /// Initializes a new instance of <see cref="FeatureCollection"/>.
    /// </summary>
    public PooledFeatureCollection(object state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _state = state;
    }

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public bool IsEmpty
    {
        get
        {
            if (_features.Count > 0)
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

            return _features.TryGetValue(key, out var result) ? result : _defaults?[key];
        }
        set
        {
            ArgumentNullException.ThrowIfNull(key);

            if (value == null)
            {
                if (_features.Remove(key))
                {
                    _containerRevision++;
                }
                return;
            }

            if (value is IPooledFeature pooledFeature)
            {
                pooledFeature.Initialize(_state);
            }

            _features[key] = value;
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
                    $"{typeof(TFeature).FullName} does not exist in the feature collection " +
                    $"and because it is a struct the method can't return null. " +
                    $"Use 'featureCollection[typeof({typeof(TFeature).FullName})] is not null' " +
                    $"to check if the feature exists.");
            }
            return (TFeature?)feature;
        }

        return (TFeature?)this[typeof(TFeature)];
    }

    /// <inheritdoc />
    public bool TryGet<TFeature>([NotNullWhen(true)] out TFeature? feature)
    {
        if (_features is not null && _features.TryGetValue(typeof(TFeature), out var result))
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

    /// <summary>
    /// Initializes the feature collection with the specified defaults.
    /// </summary>
    /// <param name="defaults">
    /// The defaults for the feature collection.
    /// </param>
    public void Initialize(IFeatureCollection? defaults = null)
    {
        _defaults = defaults;

        foreach (var pooledFeature in _pooledFeatures)
        {
            _features.Add(pooledFeature.Key, pooledFeature.Value);
            Unsafe.As<IPooledFeature>(pooledFeature.Value).Initialize(_state);
        }

        _pooledFeatures.Clear();
    }

    public void Reset()
    {
        foreach (var item in _features)
        {
            if (item.Value is IPooledFeature pooledFeature)
            {
                _pooledFeatures.Add(item);
                pooledFeature.Reset();
            }
        }

        _features.Clear();
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
    {
        if (_features != null)
        {
            foreach (var pair in _features)
            {
                yield return pair;
            }
        }

        if (_defaults != null)
        {
            // Don't return features masked by the wrapper.
            foreach (var pair in _features == null ? _defaults : _defaults.Except(_features, s_featureKeyComparer))
            {
                yield return pair;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class KeyComparer : IEqualityComparer<KeyValuePair<Type, object>>
    {
        public bool Equals(KeyValuePair<Type, object> x, KeyValuePair<Type, object> y) =>
            x.Key.Equals(y.Key);

        public int GetHashCode(KeyValuePair<Type, object> obj) =>
            obj.Key.GetHashCode();
    }
}
