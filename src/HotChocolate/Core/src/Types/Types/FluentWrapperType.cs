#nullable enable

namespace HotChocolate.Types;

public abstract class FluentWrapperType : IOutputType, IInputType
{
    Type IHasRuntimeType.RuntimeType => throw new NotSupportedException();

    TypeKind IType.Kind => throw new NotSupportedException();
}
