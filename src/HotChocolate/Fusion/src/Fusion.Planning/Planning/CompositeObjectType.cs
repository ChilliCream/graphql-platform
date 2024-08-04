using HotChocolate.Fusion.Planning.Collections;
using HotChocolate.Fusion.Planning.Completion;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

public sealed class CompositeObjectType(
    string name,
    string? description,
    CompositeObjectFieldCollection fields)
    : CompositeComplexType(name, description, fields)
{
    public override TypeKind Kind => TypeKind.Object;

    internal void Complete(CompositeObjectTypeCompletionContext context)
    {
        Directives = context.Directives;
        Implements = context.Interfaces;
        base.Complete();
    }

    public override string ToString() => Name;
}
