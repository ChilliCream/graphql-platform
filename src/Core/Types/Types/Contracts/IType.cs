namespace HotChocolate.Types
{
    public interface IType
        : ITypeSystemMember
    {
        TypeKind Kind { get; }
    }
}
