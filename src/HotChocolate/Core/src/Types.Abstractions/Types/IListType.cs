namespace HotChocolate.Types;

public interface IListType : IType
{
    IType ElementType { get; }
}
