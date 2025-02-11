using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

public sealed class FusionObjectType(
    string name,
    string? description,
    CompositeOutputFieldCollection fields)
    : FusionComplexType(name, description, fields)
{
    private bool _isEntity;

    public override TypeKind Kind => TypeKind.Object;

    public override bool IsEntity => _isEntity;

    public new ISourceComplexTypeCollection<SourceObjectType> Sources { get; private set; } = default!;

    internal void Complete(CompositeObjectTypeCompletionContext context)
    {
        Directives = context.Directives;
        Implements = context.Interfaces;
        Sources = context.Sources;
        base.Sources = context.Sources;
        _isEntity = Sources.Any(t => t.Lookups.Length > 0);

        base.Complete();
    }

    public override string ToString() => Name;
}
