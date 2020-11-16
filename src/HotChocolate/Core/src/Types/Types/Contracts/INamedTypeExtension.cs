using HotChocolate.Configuration;

namespace HotChocolate.Types
{
    public interface INamedTypeExtension
        : ITypeSystemMember
        , IHasName
    {
        TypeKind Kind { get; }
    }

    internal interface INamedTypeExtensionMerger
        : INamedTypeExtension
    {
        void Merge(ITypeCompletionContext context, INamedType type);
    }
}
