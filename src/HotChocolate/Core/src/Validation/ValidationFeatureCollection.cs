// This code was originally forked of https://github.com/dotnet/aspnetcore/tree/c7aae8ff34dce81132d0fb3a976349dcc01ff903/src/Extensions/Features/src

// ReSharper disable NonAtomicCompoundOperator
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;

namespace HotChocolate.Validation;

/// <summary>
/// Validation feature collection implementation for <see cref="IFeatureCollection"/>.
/// </summary>
internal sealed class ValidationFeatureCollection : IFeatureCollection
{
    private static readonly KeyComparer _featureKeyComparer = new();
    private Dictionary<Type, object>? _features;
    private volatile int _containerRevision;

    /// <summary>
    /// Initializes a new instance of <see cref=" ValidationFeatureCollection"/>.
    /// </summary>
    public ValidationFeatureCollection()
    {
    }

    internal IFeatureCollection? Parent { get; set; }

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public bool IsEmpty
    {
        get
        {
            if (_features is not null)
            {
                return _features.Count == 0;
            }

            if (Parent is not null)
            {
                return Parent.IsEmpty;
            }

            return true;
        }
    }

    /// <inheritdoc />
    public int Revision => _containerRevision + (Parent?.Revision ?? 0);

    /// <inheritdoc />
    public object? this[Type key]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(key);

            return _features != null && _features.TryGetValue(key, out var result) ? result : Parent?[key];
        }
        set
        {
            ArgumentNullException.ThrowIfNull(key);

            if (value == null)
            {
                if (_features?.Remove(key) == true)
                {
                    _containerRevision++;
                }
                return;
            }

            _features ??= [];
            _features[key] = value;
            _containerRevision++;

            if(value is ValidatorFeature validatorFeature
                && _features.TryGetValue(typeof(DocumentValidatorContext), out var context))
            {
                validatorFeature.OnInitialize((DocumentValidatorContext)context);
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

        if (Parent is not null && Parent.TryGet(out feature))
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
        if (_features != null)
        {
            foreach (var pair in _features)
            {
                yield return pair;
            }
        }

        if (Parent != null)
        {
            // Don't return features masked by the wrapper.
            foreach (var pair in _features == null ? Parent : Parent.Except(_features, _featureKeyComparer))
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
