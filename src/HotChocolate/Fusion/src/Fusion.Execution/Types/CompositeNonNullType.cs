using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

public sealed class CompositeNonNullType : ICompositeType
{
    public CompositeNonNullType(ICompositeType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if(type.Kind == TypeKind.NonNull)
        {
            throw new ArgumentException(
                "A non-null type cannot wrap another non-null type.",
                nameof(type));
        }

        Type = type;
    }

    public TypeKind Kind => TypeKind.NonNull;

    public ICompositeType Type { get; }
}
