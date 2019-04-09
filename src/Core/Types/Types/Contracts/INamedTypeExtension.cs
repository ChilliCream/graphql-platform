namespace HotChocolate.Types
{
    public interface INamedTypeExtension
        : IHasName
    {
        TypeKind Kind { get; }
    }

    internal interface INamedTypeExtensionMerger
        : INamedTypeExtension
    {
        void Merge(INamedType type);
    }
}
