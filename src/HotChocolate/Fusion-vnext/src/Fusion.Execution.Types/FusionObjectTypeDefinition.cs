using System.Runtime.CompilerServices;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

public sealed class FusionObjectTypeDefinition(
    string name,
    string? description,
    FusionOutputFieldDefinitionCollection fieldsDefinition)
    : FusionComplexTypeDefinition(name, description, fieldsDefinition)
    , IObjectTypeDefinition
{
    private bool _isEntity;

    public override TypeKind Kind => TypeKind.Object;

    public override bool IsEntity => _isEntity;

    public new ISourceComplexTypeCollection<SourceObjectType> Sources
        => Unsafe.As<ISourceComplexTypeCollection<SourceObjectType>>(base.Sources);

    internal void Complete(CompositeObjectTypeCompletionContext context)
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

        return other is FusionObjectTypeDefinition otherObject
            && otherObject.Name.Equals(Name, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool IsAssignableFrom(ITypeDefinition type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type.Kind == TypeKind.Object)
        {
            return Equals(type, TypeComparison.Reference);
        }

        return false;
    }

    /// <summary>
    /// Creates a <see cref="ObjectTypeDefinitionNode"/> from a
    /// <see cref="FusionObjectTypeDefinition"/>.
    /// </summary>
    public new ObjectTypeDefinitionNode ToSyntaxNode()
        => (ObjectTypeDefinitionNode)base.ToSyntaxNode();
}
