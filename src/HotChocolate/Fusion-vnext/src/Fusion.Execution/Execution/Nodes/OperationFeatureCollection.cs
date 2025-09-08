using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;

namespace HotChocolate.Fusion.Execution.Nodes;

[SuppressMessage("ReSharper", "NonAtomicCompoundOperator")]
internal sealed class OperationFeatureCollection : IFeatureCollection
{
#if NET9_0_OR_GREATER
    private readonly Lock _writeLock = new();
#else
    private readonly object _writeLock = new();
#endif
    private ImmutableDictionary<Type, object> _features = ImmutableDictionary<Type, object>.Empty;
    private volatile int _containerRevision;

    /// <summary>
    /// Initializes a new instance of <see cref="FeatureCollection"/>.
    /// </summary>
    public OperationFeatureCollection()
    {
    }

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public bool IsEmpty => _features.Count == 0;

    /// <inheritdoc />
    public int Revision => _containerRevision;

    /// <inheritdoc />
    public object? this[Type key]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(key);

            return _features.GetValueOrDefault(key);
        }
        set
        {
            ArgumentNullException.ThrowIfNull(key);

            lock (_writeLock)
            {
                if (value == null)
                {
                    _features = _features.Remove(key);
                    _containerRevision++;
                    return;
                }

                _features = _features.SetItem(key,  value);
                _containerRevision++;
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
        if (_features.TryGetValue(typeof(TFeature), out var result)
            && result is TFeature f)
        {
            feature = f;
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
        => _features.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
