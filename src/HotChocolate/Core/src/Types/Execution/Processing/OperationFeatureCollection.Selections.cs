using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Execution.Processing;

[SuppressMessage("ReSharper", "NonAtomicCompoundOperator")]
public sealed partial class OperationFeatureCollection
{
    internal object? this[int selectionId, Type featureType]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(selectionId, 0);
            ArgumentNullException.ThrowIfNull(featureType);

            return _selectionFeatures.GetValueOrDefault((selectionId, featureType));
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(selectionId, 0);
            ArgumentNullException.ThrowIfNull(featureType);

            lock (_writeLock)
            {
                if (value == null)
                {
                    _selectionFeatures = _selectionFeatures.Remove((selectionId, featureType));
                    _containerRevision++;
                    return;
                }

                _selectionFeatures = _selectionFeatures.SetItem((selectionId, featureType), value);
                _containerRevision++;
            }
        }
    }

    internal bool TryGet<TFeature>(int selectionId, [NotNullWhen(true)] out TFeature? feature)
    {
        if (_selectionFeatures.TryGetValue((selectionId, typeof(TFeature)), out var result)
            && result is TFeature f)
        {
            feature = f;
            return true;
        }

        feature = default;
        return false;
    }

    internal TFeature GetOrSetSafe<TFeature>(int selectionId, Func<TFeature> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        if (!TryGet<TFeature>(selectionId, out var feature))
        {
            lock (_writeLock)
            {
                if (!TryGet(selectionId, out feature))
                {
                    feature = factory();
                    this[selectionId, typeof(TFeature)] = feature;
                }
            }
        }

        return feature;
    }

    internal TFeature GetOrSetSafe<TFeature, TContext>(
        int selectionId,
        Func<TContext, TFeature> factory,
        TContext context)
    {
        ArgumentNullException.ThrowIfNull(factory);

        if (!TryGet<TFeature>(selectionId, out var feature))
        {
            lock (_writeLock)
            {
                if (!TryGet(selectionId, out feature))
                {
                    feature = factory(context);
                    this[selectionId, typeof(TFeature)] = feature;
                }
            }
        }

        return feature;
    }

    internal IEnumerable<KeyValuePair<Type, object>> GetFeatures(int selectionId)
    {
        foreach (var ((id, type), value) in _selectionFeatures)
        {
            if (selectionId == id)
            {
                yield return new KeyValuePair<Type, object>(type, value);
            }
        }
    }

    internal bool HasFeatures(int selectionId)
        => !_selectionFeatures.IsEmpty
            && _selectionFeatures.Keys.Any(t => t.Item1 == selectionId);
}
