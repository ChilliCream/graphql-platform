using System.Runtime.CompilerServices;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Fusion.Types.Metadata;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

/// <summary>
/// Represents a GraphQL interface type definition in a fusion schema.
/// </summary>
public sealed class FusionInterfaceTypeDefinition(
    string name,
    string? description,
    bool isInaccessible,
    FusionOutputFieldDefinitionCollection fieldsDefinition)
    : FusionComplexTypeDefinition(name, description, isInaccessible, fieldsDefinition)
    , IInterfaceTypeDefinition
{
    /// <inheritdoc />
    public override TypeKind Kind => TypeKind.Interface;

    /// <summary>
    /// Gets metadata about this interface type in its source schemas.
    /// Each entry in the collection provides information about this interface type
    /// that is specific to the source schemas the type was composed of.
    /// </summary>
    public new ISourceComplexTypeCollection<SourceInterfaceType> Sources
        => Unsafe.As<ISourceComplexTypeCollection<SourceInterfaceType>>(base.Sources);

    internal void Complete(CompositeInterfaceTypeCompletionContext context)
    {
        if (context.Directives is null || context.Interfaces is null
            || context.Sources is null || context.Features is null)
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
