using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

public sealed class CompositeListType : ICompositeType
{
    public CompositeListType(ICompositeType type)
    {
        ArgumentNullException.ThrowIfNull(type);
        Type = type;
    }

    public TypeKind Kind => TypeKind.List;

    public ICompositeType Type { get; }
}
