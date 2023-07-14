using System.Collections;

namespace HotChocolate.Fusion.Composition.Features;

public sealed class FusionFeatureCollection : IReadOnlyList<IFusionFeature>
{
    private readonly Dictionary<Type, IFusionFeature> _features = new();

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

    public IFusionFeature this[int index] => throw new NotImplementedException();

    public bool IsSupported<TFeature>(IFusionFeature feature) where TFeature : IFusionFeature
        => _features.TryGetValue(typeof(TFeature), out var registeredFeature) &&
            registeredFeature.Equals(feature);

    public bool IsSupported<TFeature>() where TFeature : IFusionFeature
        => _features.ContainsKey(typeof(TFeature));

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

    public int Count => _features.Count;

    public IEnumerator<IFusionFeature> GetEnumerator()
        => _features.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
    
    public static FusionFeatureCollection Empty { get; } = new(Array.Empty<IFusionFeature>());
}