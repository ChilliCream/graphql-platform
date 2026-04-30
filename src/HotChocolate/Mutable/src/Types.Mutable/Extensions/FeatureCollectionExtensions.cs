using HotChocolate.Features;

namespace HotChocolate.Types.Mutable;

public static class FeatureCollectionExtensions
{
    /// <summary>
    /// Marks a type definition as a type extension by setting the
    /// <see cref="TypeExtensionMarker"/> feature, which is consumed by
    /// the canonical SDL formatter to emit the type using <c>extend</c> syntax.
    /// </summary>
    public static void MarkAsExtension<T>(this T type)
        where T : ITypeDefinition, IFeatureProvider
        => type.Features.Set(TypeExtensionMarker.Instance);
}
