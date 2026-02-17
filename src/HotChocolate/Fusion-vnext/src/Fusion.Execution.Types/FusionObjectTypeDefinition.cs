using System.Runtime.CompilerServices;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Fusion.Types.Metadata;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

/// <summary>
/// Represents a GraphQL object type definition in a fusion schema.
/// </summary>
public sealed class FusionObjectTypeDefinition(
    string name,
    string? description,
    bool isInaccessible,
    FusionOutputFieldDefinitionCollection fieldsDefinition)
    : FusionComplexTypeDefinition(name, description, isInaccessible, fieldsDefinition)
    , IObjectTypeDefinition
{
    /// <inheritdoc />
    public override TypeKind Kind => TypeKind.Object;

    /// <summary>
    /// Gets metadata about this object type in its source schemas.
    /// Each entry in the collection provides information about this object type
    /// that is specific to the source schemas the type was composed of.
    /// </summary>
    public new ISourceComplexTypeCollection<SourceObjectType> Sources
        => Unsafe.As<ISourceComplexTypeCollection<SourceObjectType>>(base.Sources);

    internal void Complete(CompositeObjectTypeCompletionContext context)
    {
        if (context.Directives is null
            || context.Interfaces is null
            || context.Sources is null
            || context.Features is null)
        {
            throw ThrowHelper.InvalidCompletionContext();
        }

        Directives = context.Directives;
        Implements = context.Interfaces;
        base.Sources = context.Sources;
        Features = context.Features;

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
