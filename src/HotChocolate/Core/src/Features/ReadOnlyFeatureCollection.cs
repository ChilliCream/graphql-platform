using System.Collections;
using System.Collections.Frozen;

namespace HotChocolate.Features;

/// <summary>
/// Read-only implementation for <see cref="IFeatureCollection"/>.
/// </summary>
public sealed class ReadOnlyFeatureCollection : IFeatureCollection
{
    private readonly FrozenDictionary<Type, object> _features;
    private volatile int _containerRevision;

    /// <summary>
    /// Initializes a new instance of <see cref="ReadOnlyFeatureCollection"/>.
    /// </summary>
    /// <param name="features">
    /// The <see cref="IFeatureCollection"/> to make readonly.
    /// </param>
    public ReadOnlyFeatureCollection(IFeatureCollection features)
    {
        _features = features.ToFrozenDictionary();

        foreach (var feature in _features.Values)
        {
            if (feature is ISealable sealable)
            {
                sealable.Seal();
            }
        }

        _containerRevision = features.Revision;
    }

    /// <inheritdoc />
    public bool IsReadOnly => true;

    /// <inheritdoc />
    public int Revision => _containerRevision;

    /// <inheritdoc />
    public object? this[Type key]
    {
        get => _features.TryGetValue(key, out var value) ? value : null;
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
        => throw new NotSupportedException("The feature collection is read-only.");

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<Type, object>> GetEnumerator() => _features.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
