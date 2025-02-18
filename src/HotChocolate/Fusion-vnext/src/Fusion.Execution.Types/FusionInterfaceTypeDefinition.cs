using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

public sealed class FusionInterfaceTypeDefinition(
    string name,
    string? description,
    FusionOutputFieldDefinitionCollection fieldsDefinition)
    : FusionComplexTypeDefinition(name, description, fieldsDefinition)
    , IInterfaceTypeDefinition
{
    private bool _isEntity;

    public override TypeKind Kind => TypeKind.Object;

    public override bool IsEntity => _isEntity;

    public new ISourceComplexTypeCollection<SourceInterfaceType> Sources { get; private set; } = null!;

    internal void Complete(CompositeInterfaceTypeCompletionContext context)
    {
        Directives = context.Directives;
        Implements = context.Interfaces;
        Sources = context.Sources;
        base.Sources = context.Sources;
        _isEntity = Sources.Any(t => t.Lookups.Length > 0);

        Complete();
    }

    /// <inheritdoc />
    public override bool Equals(IType? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }

        return other is FusionInterfaceTypeDefinition otherInterface
            && otherInterface.Name.Equals(Name, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool IsAssignableFrom(ITypeDefinition type)
    {
        switch (type.Kind)
        {
            case TypeKind.Interface:
                return Equals(type, TypeComparison.Reference)
                    || ((IInterfaceTypeDefinition)type).Implements.ContainsName(Name);

            case TypeKind.Object:
                return ((IObjectTypeDefinition)type).Implements.ContainsName(Name);

            default:
                return false;
        }
    }

    /// <summary>
    /// Creates a <see cref="InterfaceTypeDefinitionNode"/> from a
    /// <see cref="FusionInterfaceTypeDefinition"/>.
    /// </summary>
    public new InterfaceTypeDefinitionNode ToSyntaxNode()
        => (InterfaceTypeDefinitionNode)base.ToSyntaxNode();
}
