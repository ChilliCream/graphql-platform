using HotChocolate.Features;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types.Mutable;

public static class TypeExtensions
{
    public static bool IsTypeExtension(this IType type)
        => type is IFeatureProvider featureProvider
            && featureProvider.Features.Get<TypeMetadata>() is { IsExtension: true };
}
