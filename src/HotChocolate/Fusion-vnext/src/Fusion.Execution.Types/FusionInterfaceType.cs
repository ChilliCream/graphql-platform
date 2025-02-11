using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

public sealed class FusionInterfaceType(
    string name,
    string? description,
    FusionOutputFieldDefinitionCollection fieldsDefinition)
    : FusionComplexType(name, description, fieldsDefinition)
{
    private bool _isEntity;

    public override TypeKind Kind => TypeKind.Object;

    public override bool IsEntity => _isEntity;

    public new ISourceComplexTypeCollection<SourceInterfaceType> Sources { get; private set; } = null!;

    public bool IsAssignableFrom(ICompositeNamedType type)
    {
        switch (type.Kind)
        {
            case TypeKind.Interface:
                return ReferenceEquals(type, this) || ((FusionInterfaceType)type).Implements.ContainsName(Name);

            case TypeKind.Object:
                return ((FusionObjectType)type).Implements.ContainsName(Name);

            default:
                return false;
        }
    }

    internal void Complete(CompositeInterfaceTypeCompletionContext context)
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
