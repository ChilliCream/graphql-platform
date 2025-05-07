using System.Runtime.CompilerServices;
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

    /// <inheritdoc />
    public override TypeKind Kind => TypeKind.Object;

    /// <inheritdoc />
    public SchemaCoordinate Coordinate => new(Name, ofDirective: false);

    /// <summary>
    /// Defines is this interface is an entity.
    /// </summary>
    public override bool IsEntity => _isEntity;

    /// <summary>
    /// Gets source schema metadata for this interface.
    /// </summary>
    public new ISourceComplexTypeCollection<SourceInterfaceType> Sources
        => Unsafe.As<ISourceComplexTypeCollection<SourceInterfaceType>>(base.Sources);

    internal void Complete(CompositeInterfaceTypeCompletionContext context)
    {
        Directives = context.Directives;
        Implements = context.Interfaces;
        base.Sources = context.Sources;
        Features = context.Features;
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
        ArgumentNullException.ThrowIfNull(type);

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
