using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

public sealed class CompositeObjectType(
    string name,
    string? description,
    CompositeOutputFieldCollection fields)
    : CompositeComplexType(name, description, fields)
{
    public override TypeKind Kind => TypeKind.Object;

    public new ISourceComplexTypeCollection<SourceObjectType> Sources { get; private set; } = default!;

    public bool IsEntity { get; private set; }

    internal void Complete(CompositeObjectTypeCompletionContext context)
    {
        Directives = context.Directives;
        Implements = context.Interfaces;
        Sources = context.Sources;
        base.Sources = context.Sources;
        IsEntity = Sources.Any(t => t.Lookups.Length > 0);

        base.Complete();
    }

    public override string ToString() => Name;
}
