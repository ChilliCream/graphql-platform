using HotChocolate.Configuration;

namespace HotChocolate.Types
{
    public interface INamedTypeExtension
        : ITypeSystem
        , IHasName
    {
        TypeKind Kind { get; }
    }

    internal interface INamedTypeExtensionMerger
        : INamedTypeExtension
    {
        void Merge(ICompletionContext context, INamedType type);
    }
}
