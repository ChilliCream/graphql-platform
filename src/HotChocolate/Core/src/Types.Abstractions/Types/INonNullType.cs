namespace HotChocolate.Types;

public interface INonNullType : IType
{
    IType NullableType { get; }
}
