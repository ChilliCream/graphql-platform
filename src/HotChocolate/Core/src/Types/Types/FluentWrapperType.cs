#nullable enable

namespace HotChocolate.Types;

public abstract class FluentWrapperType : IOutputType, IInputType
{
    TypeKind IType.Kind => throw new NotSupportedException();

    public bool Equals(IType? other) => ReferenceEquals(this, other);
}
