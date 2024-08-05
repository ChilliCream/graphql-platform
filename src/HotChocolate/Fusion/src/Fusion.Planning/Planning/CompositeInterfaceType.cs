using HotChocolate.Fusion.Planning.Collections;
using HotChocolate.Fusion.Planning.Completion;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

public sealed class CompositeInterfaceType(
    string name,
    string? description,
    CompositeObjectFieldCollection fields)
    : CompositeComplexType(name, description, fields)
{
    public override TypeKind Kind => TypeKind.Object;

    public bool IsAssignableFrom(ICompositeNamedType type)
    {
        switch (type.Kind)
        {
            case TypeKind.Interface:
                return ReferenceEquals(type, this) || ((CompositeInterfaceType)type).Implements.ContainsName(Name);

            case TypeKind.Object:
                return ((CompositeObjectType)type).Implements.ContainsName(Name);

            default:
                return false;
        }
    }

    internal void Complete(CompositeInterfaceTypeCompletionContext context)
    {
        Directives = context.Directives;
        Implements = context.Interfaces;
    }
}
