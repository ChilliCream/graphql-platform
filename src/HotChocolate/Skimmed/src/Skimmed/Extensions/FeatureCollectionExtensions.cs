namespace HotChocolate.Skimmed;

public static class FeatureCollectionExtensions
{
    public static TypeMetadata GetTypeMetadata(this INamedTypeDefinition type)
    {
        var metadata = type.Features.Get<TypeMetadata>();

        if(metadata is null)
        {
            metadata = new TypeMetadata();
            type.Features.Set(metadata);
        }

        return metadata;
    }
}
