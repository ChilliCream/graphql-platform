using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Features;

/// <summary>
/// Read-only implementation for <see cref="IFeatureCollection"/>.
/// </summary>
public sealed class ReadOnlyFeatureCollection : IFeatureCollection
{
    private const int MaxItemsCount = 8;
    private readonly KeyValuePair<Type, object>[]? _items;
    private readonly FrozenDictionary<Type, object>? _features;
    private readonly int _containerRevision;

    /// <summary>
    /// Initializes a new instance of <see cref="ReadOnlyFeatureCollection"/>.
    /// </summary>
    /// <param name="features">
    /// The <see cref="IFeatureCollection"/> to make readonly.
    /// </param>
    public ReadOnlyFeatureCollection(IFeatureCollection features)
    {
        ArgumentNullException.ThrowIfNull(features);

        if (features is FeatureCollection featureCollection
            && featureCollection.TryCopyFeatures(MaxItemsCount, out var items))
        {
            _items = items;

            foreach (var item in items)
            {
                Seal(item.Value);
            }
        }
        else
        {
            var frozenFeatures = features.ToFrozenDictionary();
            _features = frozenFeatures;

            // todo: this has an issues as it will also seal the defaults.
            foreach (var feature in frozenFeatures.Values)
            {
                Seal(feature);
            }
        }

        _containerRevision = features.Revision;
    }

    /// <inheritdoc />
    public bool IsReadOnly => true;

    /// <inheritdoc />
    public bool IsEmpty => (_items?.Length ?? _features!.Count) == 0;

    /// <inheritdoc />
    public int Revision => _containerRevision;

    /// <inheritdoc />
    public object? this[Type key]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(key);

            return TryGetValue(key, out var value) ? value : null;
        }
        set => throw new NotSupportedException("The feature collection is read-only.");
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
        if (TryGetValue(typeof(TFeature), out var result))
        {
            if (result is TFeature f)
            {
                feature = f;
                return true;
            }

            feature = default;
            return false;
        }

        feature = default;
        return false;
    }

    /// <inheritdoc />
    public void Set<TFeature>(TFeature? instance)
        => throw new NotSupportedException("The feature collection is read-only.");

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
        => _items is { } items
            ? ((IEnumerable<KeyValuePair<Type, object>>)items).GetEnumerator()
            : _features!.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private bool TryGetValue(Type key, [NotNullWhen(true)] out object? value)
    {
        if (_items is { } items)
        {
            foreach (var item in items)
            {
                if (item.Key == key)
                {
                    value = item.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        return _features!.TryGetValue(key, out value);
    }

    private static void Seal(object feature)
    {
        if (feature is ISealable { IsReadOnly: false } sealable)
        {
            sealable.Seal();
        }
    }
}
