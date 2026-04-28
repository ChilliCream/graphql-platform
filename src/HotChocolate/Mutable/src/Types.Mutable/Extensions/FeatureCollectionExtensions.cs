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

    /// <summary>
    /// Marks a type definition as a type extension by setting both
    /// <see cref="TypeMetadata.IsExtension"/> and the cross-cutting
    /// <see cref="TypeExtensionMarker"/> feature consumed by the canonical SDL formatter.
    /// </summary>
    public static void MarkAsExtension<T>(this T type)
        where T : ITypeDefinition, IFeatureProvider
    {
        type.GetTypeMetadata().IsExtension = true;
        type.Features.Set(TypeExtensionMarker.Instance);
    }
}
