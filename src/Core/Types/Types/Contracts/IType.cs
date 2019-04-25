namespace HotChocolate.Types
{
    public interface IType
        : ITypeSystem
    {
        TypeKind Kind { get; }
    }
}
