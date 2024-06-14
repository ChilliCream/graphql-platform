using HotChocolate.Features;

namespace HotChocolate.Skimmed;

public static class FeatureCollectionExtensions
{
    public static void ModifyFeature<T>(this IFeatureCollection features, Action<T> modification)
        where T : class, new()
    {
        var feature = features.Get<T>();

        if(feature is null)
        {
            feature = new T();
            features.Set(feature);
        }

        modification(feature);
    }
}
