using HotChocolate.Features;

namespace HotChocolate.Types.Mutable;

public static class FeatureCollectionExtensions
{
    public static TypeMetadata GetTypeMetadata<T>(this T type)
        where T : ITypeDefinition, IFeatureProvider
    {
        var metadata = type.Features.Get<TypeMetadata>();

        if (metadata is null)
        {
            metadata = new TypeMetadata();
            type.Features.Set(metadata);
        }

        return metadata;
    }
}
