using HotChocolate.Fusion.Planning.Collections;
using HotChocolate.Fusion.Planning.Completion;
using HotChocolate.Types;
using Microsoft.VisualBasic.CompilerServices;

namespace HotChocolate.Fusion.Planning;

public sealed class CompositeObjectType(
    string name,
    string? description,
    CompositeObjectFieldCollection fields)
    : CompositeComplexType(name, description, fields)
{
    public override TypeKind Kind => TypeKind.Object;

    public SourceObjectTypeCollection Sources { get; private set; } = default!;

    internal void Complete(CompositeObjectTypeCompletionContext context)
    {
        Directives = context.Directives;
        Implements = context.Interfaces;
        Sources = context.Sources;
        base.Complete();
    }

    public override string ToString() => Name;
}
