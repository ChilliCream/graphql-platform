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

    internal void Complete(CompositeInterfaceTypeCompletionContext context)
    {
        Directives = context.Directives;
        Implements = context.Interfaces;
    }
}
