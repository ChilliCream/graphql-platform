#nullable enable

namespace HotChocolate.Types;

public abstract class NonNamedType
    : IOutputType
    , IInputType
{
    private Type? _innerClrType;
    private Type? _clrType;

    protected NonNamedType(IType innerType)
    {
        InnerType = innerType ?? throw new ArgumentNullException(nameof(innerType));
    }

    public abstract TypeKind Kind { get; }

    protected IType InnerType { get; }

    protected Type InnerClrType
    {
        get
        {
            return _innerClrType ??= InnerType.ToRuntimeType();
        }
    }
    public Type RuntimeType
    {
        get
        {
            return _clrType ??= this.ToRuntimeType();
        }
    }
}
