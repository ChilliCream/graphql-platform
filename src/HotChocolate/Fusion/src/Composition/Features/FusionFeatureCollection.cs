using System.Collections;

namespace HotChocolate.Fusion.Composition.Features;

/// <summary>
/// Represents a collection of fusion composition features.
/// </summary>
public sealed class FusionFeatureCollection : IReadOnlyCollection<IFusionFeature>
{
    private readonly Dictionary<Type, IFusionFeature> _features = new();

    /// <summary>
    /// Initializes a new instance of <see cref="FusionFeatureCollection"/>.
    /// </summary>
    /// <param name="features">
    /// The features that are supported by composition.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="features"/> is <c>null</c>.
    /// </exception>
    public FusionFeatureCollection(params IFusionFeature[] features)
    {
        if (features == null)
        {
            throw new ArgumentNullException(nameof(features));
        }

        foreach (var feature in features)
        {
            _features[feature.GetType()] = feature;
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FusionFeatureCollection"/>.
    /// </summary>
    /// <param name="features">
    /// The features that are supported by composition.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="features"/> is <c>null</c>.
    /// </exception>
    public FusionFeatureCollection(IEnumerable<IFusionFeature> features)
    {
        if (features == null)
        {
            throw new ArgumentNullException(nameof(features));
        }

        foreach (var feature in features)
        {
            _features[feature.GetType()] = feature;
        }
    }

    /// <summary>
    /// Specifies if the specified feature is supported.
    /// </summary>
    /// <param name="feature">
    /// The feature that shall be checked.
    /// </param>
    /// <typeparam name="TFeature">
    /// The type of the feature that shall be checked.
    /// </typeparam>
    /// <returns>
    /// <c>true</c> if the specified feature is supported; otherwise, <c>false</c>.
    /// </returns>
    public bool IsSupported<TFeature>(IFusionFeature feature) where TFeature : IFusionFeature
        => _features.TryGetValue(typeof(TFeature), out var registeredFeature) &&
            registeredFeature.Equals(feature);

    /// <summary>
    /// Specifies if the specified feature is supported.
    /// </summary>
    /// <typeparam name="TFeature">
    /// The type of the feature that shall be checked.
    /// </typeparam>
    /// <returns></returns>
    public bool IsSupported<TFeature>() where TFeature : IFusionFeature
        => _features.ContainsKey(typeof(TFeature));

    /// <summary>
    /// Checks if a feature is supported and returns it's configured instance.
    /// </summary>
    /// <param name="feature">
    /// The configured feature instance.
    /// </param>
    /// <typeparam name="TFeature">
    /// The type of the feature that shall be checked.
    /// </typeparam>
    /// <returns>
    /// <c>true</c> if the specified feature is configured; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetFeature<TFeature>(out TFeature feature) where TFeature : IFusionFeature
    {
        if (_features.TryGetValue(typeof(TFeature), out var registeredFeature))
        {
            feature = (TFeature) registeredFeature;
            return true;
        }

        feature = default!;
        return false;
    }

    /// <summary>
    /// Gets the number of features that are configured.
    /// </summary>
    public int Count => _features.Count;

    /// <summary>
    /// Gets an enumerator for this feature collection.
    /// </summary>
    /// <returns></returns>
    public IEnumerator<IFusionFeature> GetEnumerator()
        => _features.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <summary>
    /// Gets an empty feature collection.
    /// </summary>
    public static FusionFeatureCollection Empty { get; } = new([]);
}
