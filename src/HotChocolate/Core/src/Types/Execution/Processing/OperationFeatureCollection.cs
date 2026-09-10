using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;

namespace HotChocolate.Execution.Processing;

[SuppressMessage("ReSharper", "NonAtomicCompoundOperator")]
public sealed partial class OperationFeatureCollection : IFeatureCollection
{
#if NET9_0_OR_GREATER
    private readonly Lock _writeLock = new();
#else
    private readonly object _writeLock = new();
#endif
#if NET10_0_OR_GREATER
    private ImmutableDictionary<Type, object> _features = [];
    private ImmutableDictionary<(int, Type), object> _selectionFeatures = [];
#else
    private ImmutableDictionary<Type, object> _features = ImmutableDictionary<Type, object>.Empty;
    private ImmutableDictionary<(int, Type), object> _selectionFeatures = ImmutableDictionary<(int, Type), object>.Empty;
#endif
    private volatile int _containerRevision;

    /// <summary>
    /// Initializes a new instance of <see cref="FeatureCollection"/>.
    /// </summary>
    internal OperationFeatureCollection()
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

                _features = _features.SetItem(key, value);
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

    /// <summary>
    /// Gets an existing feature or creates and sets a new instance using the default constructor.
    /// </summary>
    /// <typeparam name="TFeature">The type of the feature.</typeparam>
    /// <returns>The existing or newly created feature instance.</returns>
    /// <remarks>
    /// This method is thread-safe. Under contention more than one instance can be created,
    /// but only one instance is stored and every caller observes that instance.
    /// </remarks>
    public TFeature GetOrSetSafe<TFeature>() where TFeature : new()
        => GetOrSetSafe(static () => new TFeature());

    /// <summary>
    /// Gets an existing feature or creates and sets a new instance using the
    /// specified <paramref name="factory"/>.
    /// </summary>
    /// <typeparam name="TFeature">The type of the feature.</typeparam>
    /// <param name="factory">The factory that creates the feature instance.</param>
    /// <returns>The existing or newly created feature instance.</returns>
    /// <remarks>
    /// This method is thread-safe. Under contention the <paramref name="factory"/> can be
    /// invoked more than once, but only one instance is stored and every caller observes
    /// that instance.
    /// </remarks>
    public TFeature GetOrSetSafe<TFeature>(Func<TFeature> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        if (TryGet<TFeature>(out var feature))
        {
            return feature;
        }

        // The factory is invoked outside of the write lock, feature factories can reach back
        // into the operation and take the operation lock to compile selection sets.
        var created = factory();

        lock (_writeLock)
        {
            if (TryGet<TFeature>(out var existing))
            {
                return existing;
            }

            this[typeof(TFeature)] = created;
            return created;
        }
    }

    internal TFeature GetOrSetSafe<TFeature, TContext>(
        Func<TContext, TFeature> factory,
        TContext context)
    {
        ArgumentNullException.ThrowIfNull(factory);

        if (TryGet<TFeature>(out var feature))
        {
            return feature;
        }

        var created = factory(context);

        lock (_writeLock)
        {
            if (TryGet<TFeature>(out var existing))
            {
                return existing;
            }

            this[typeof(TFeature)] = created;
            return created;
        }
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

    /// <summary>
    /// Sets a feature instance for this selection.
    /// </summary>
    /// <typeparam name="TFeature">The type of the feature.</typeparam>
    /// <param name="instance">The feature instance to set, or <c>null</c> to remove.</param>
    /// <remarks>This method is thread-safe.</remarks>
    public void SetSafe<TFeature>(TFeature? instance)
        => this[typeof(TFeature)] = instance;

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
        => _features.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
