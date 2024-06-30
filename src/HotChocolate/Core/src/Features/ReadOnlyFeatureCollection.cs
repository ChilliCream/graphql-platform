using System.Collections;
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif

namespace HotChocolate.Features;

/// <summary>
/// Read-only implementation for <see cref="IFeatureCollection"/>.
/// </summary>
public sealed class ReadOnlyFeatureCollection : IFeatureCollection
{
#if NET8_0_OR_GREATER
    private readonly FrozenDictionary<Type, object> _features;
#else
    private readonly Dictionary<Type, object> _features;
#endif
    private volatile int _containerRevision;

    /// <summary>
    /// Initializes a new instance of <see cref="ReadOnlyFeatureCollection"/>.
    /// </summary>
    /// <param name="features">
    /// The <see cref="IFeatureCollection"/> to make readonly.
    /// </param>
    public ReadOnlyFeatureCollection(IFeatureCollection features)
    {
#if NET8_0_OR_GREATER
        _features = features.ToFrozenDictionary();
#else
        _features = features.ToDictionary(t => t.Key, t => t.Value);
#endif
        _containerRevision = features.Revision;
    }

    /// <inheritdoc />
    public bool IsReadOnly => true;

    /// <inheritdoc />
    public int Revision => _containerRevision;

    /// <inheritdoc />
    public object? this[Type key]
    {
        get => _features[key];
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
