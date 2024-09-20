// This code was originally forked of https://github.com/dotnet/aspnetcore/tree/c7aae8ff34dce81132d0fb3a976349dcc01ff903/src/Extensions/Features/src

// ReSharper disable NonAtomicCompoundOperator
using System.Collections;

namespace HotChocolate.Features;

/// <summary>
/// Default implementation for <see cref="IFeatureCollection"/>.
/// </summary>
public class FeatureCollection : IFeatureCollection
{
    private static readonly KeyComparer _featureKeyComparer = new();
    private readonly IFeatureCollection? _defaults;
    private readonly int _initialCapacity;
    private Dictionary<Type, object>? _features;
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
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <paramref name="initialCapacity"/> is less than 0
    /// </exception>
    public FeatureCollection(int initialCapacity)
    {
        if (initialCapacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialCapacity));
        }

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
    public virtual int Revision
    {
        get { return _containerRevision + (_defaults?.Revision ?? 0); }
    }

    /// <inheritdoc />
    public bool IsReadOnly { get { return false; } }

    /// <inheritdoc />
    public object? this[Type key]
    {
        get
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return _features != null && _features.TryGetValue(key, out var result) ? result : _defaults?[key];
        }
        set
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                if (_features != null && _features.Remove(key))
                {
                    _containerRevision++;
                }
                return;
            }

            if (_features == null)
            {
                _features = new Dictionary<Type, object>(_initialCapacity);
            }
            _features[key] = value;
            _containerRevision++;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
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
            foreach (var pair in _features == null ? _defaults : _defaults.Except(_features, _featureKeyComparer))
            {
                yield return pair;
            }
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
    public void Set<TFeature>(TFeature? instance)
    {
        this[typeof(TFeature)] = instance;
    }

    private sealed class KeyComparer : IEqualityComparer<KeyValuePair<Type, object>>
    {
        public bool Equals(KeyValuePair<Type, object> x, KeyValuePair<Type, object> y) =>
            x.Key.Equals(y.Key);

        public int GetHashCode(KeyValuePair<Type, object> obj) =>
            obj.Key.GetHashCode();
    }
}
