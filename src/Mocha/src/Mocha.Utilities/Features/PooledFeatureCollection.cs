using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Mocha.Features;

/// <summary>
/// A feature collection that is optimized for pooling.
/// </summary>
public sealed class PooledFeatureCollection : IFeatureCollection
{
    private static readonly KeyComparer s_featureKeyComparer = new();
    private readonly Dictionary<Type, object> _activeFeatures = [];
    private readonly Dictionary<Type, object> _cachedFeatures = [];
    private readonly object _state;
    private IFeatureCollection? _inheritedFeatures;
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
            if (_activeFeatures.Count > 0)
            {
                return false;
            }

            return _inheritedFeatures?.IsEmpty ?? true;
        }
    }

    /// <inheritdoc />
    public int Revision => _containerRevision + (_inheritedFeatures?.Revision ?? 0);

    /// <inheritdoc />
    public object? this[Type key]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(key);

            return _activeFeatures.TryGetValue(key, out var result) ? result : _inheritedFeatures?[key];
        }
        set
        {
            ArgumentNullException.ThrowIfNull(key);

            if (value == null)
            {
                if (_activeFeatures.Remove(key))
                {
                    _containerRevision++;
                }
                return;
            }

            if (value is IPooledFeature pooledFeature)
            {
                pooledFeature.Initialize(_state);
            }

            _activeFeatures[key] = value;
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
        if (_activeFeatures.TryGetValue(typeof(TFeature), out var result))
        {
            if (result is TFeature f)
            {
                feature = f;
                return true;
            }

            feature = default;
            return false;
        }

        if (_inheritedFeatures is not null && _inheritedFeatures.TryGet(out feature))
        {
            return true;
        }

        feature = default;
        return false;
    }

    /// <inheritdoc />
    public TFeature GetOrSet<TFeature, TState>(Func<TState, TFeature> factory, TState state)
    {
        if (TryGet(out TFeature? feature))
        {
            return feature;
        }

        var key = typeof(TFeature);
        if (_cachedFeatures.TryGetValue(key, out var cached) && cached is TFeature)
        {
            _cachedFeatures.Remove(key);
            this[key] = cached;
            return (TFeature)cached;
        }

        feature = factory(state);
        Set(feature);
        return feature;
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
    /// The inherited features, or <c>null</c> when the collection has no defaults.
    /// </param>
    public void Initialize(IFeatureCollection? defaults = null)
    {
        _inheritedFeatures = defaults;
    }

    /// <summary>
    /// Resets the feature collection by clearing all features and returning pooled features to their initial state.
    /// </summary>
    public void Reset()
    {
        _inheritedFeatures = null;
        foreach (var item in _activeFeatures)
        {
            if (item.Value is IPooledFeature pooledFeature)
            {
                pooledFeature.Reset();

                _cachedFeatures[item.Key] = item.Value;
            }
        }

        _activeFeatures.Clear();
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
    {
        foreach (var pair in _activeFeatures)
        {
            yield return pair;
        }

        if (_inheritedFeatures != null)
        {
            // Don't return features masked by the wrapper.
            foreach (var pair in _inheritedFeatures.Except(_activeFeatures, s_featureKeyComparer))
            {
                yield return pair;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class KeyComparer : IEqualityComparer<KeyValuePair<Type, object>>
    {
        public bool Equals(KeyValuePair<Type, object> x, KeyValuePair<Type, object> y) => x.Key.Equals(y.Key);

        public int GetHashCode(KeyValuePair<Type, object> obj) => obj.Key.GetHashCode();
    }
}
