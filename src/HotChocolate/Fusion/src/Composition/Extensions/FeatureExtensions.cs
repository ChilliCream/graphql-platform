using HotChocolate.Features;

namespace HotChocolate.Fusion.Composition;

internal static class FeatureExtensions
{
    public static T GetOrCreateFeature<T>(this IFeatureProvider featureProvider)
        where T : class, new()
    {
        var feature = featureProvider.Features.Get<T>();

        if (feature is null)
        {
            feature = new T();
            featureProvider.Features.Set(feature);
        }

        return feature;
    }

    public static FusionMemberMetadata GetMemberMetadata(this IFeatureProvider featureProvider)
        => featureProvider.GetOrCreateFeature<FusionMemberMetadata>();

    public static void SetOriginalName(this IFeatureProvider featureProvider, string originalName)
        => featureProvider.GetMemberMetadata().OriginalName = originalName;
}
