using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
/// <param name="name">The name of the object type.</param>
/// <param name="description">The description of the object type.</param>
/// <param name="deprecationReason">
/// The deprecation reason, or <c>null</c> if the object type is not deprecated.
/// An empty or white-space value is treated as <c>null</c>.
/// </param>
/// <param name="isInaccessible">A value indicating whether the type is inaccessible.</param>
/// <param name="fieldsDefinition">The collection of fields defined on this object type.</param>
public sealed class FusionObjectTypeDefinition(
    string name,
    string? description,
    string? deprecationReason,
    bool isInaccessible,
    FusionOutputFieldDefinitionCollection fieldsDefinition)
    : FusionComplexTypeDefinition(name, description, isInaccessible, fieldsDefinition)
    , IObjectTypeDefinition
{
    private FusionTypeFlags _flags;

    /// <inheritdoc />
    public override TypeKind Kind => TypeKind.Object;

    /// <inheritdoc />
    public override bool IsSharedType => (_flags & FusionTypeFlags.Shared) != 0;

    /// <inheritdoc />
    public override bool IsEntityType => (_flags & FusionTypeFlags.Entity) != 0;

    /// <summary>
    /// Defines if this object type is deprecated.
    /// This is <c>true</c> if a <see cref="DeprecationReason"/> is present.
    /// </summary>
    [MemberNotNullWhen(true, nameof(DeprecationReason))]
    public bool IsDeprecated => DeprecationReason is not null;

    /// <summary>
    /// Gets the deprecation reason, or <c>null</c> if this object type is not deprecated.
    /// </summary>
    public string? DeprecationReason { get; } =
        string.IsNullOrWhiteSpace(deprecationReason) ? null : deprecationReason;

    /// <summary>
    /// Gets metadata about this object type in its source schemas.
    /// Each entry in the collection provides information about this object type
    /// that is specific to the source schemas the type was composed of.
    /// </summary>
    public new ISourceComplexTypeCollection<SourceObjectType> Sources
        => Unsafe.As<ISourceComplexTypeCollection<SourceObjectType>>(base.Sources);

    /// <summary>
    /// Gets the authorization policy applications for this object type.
    /// </summary>
    public ImmutableArray<PolicyApplication> PolicyApplications { get; private set; }

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
        PolicyApplications = context.PolicyApplications;
        Features = context.Features;
        SetFlags(context.Sources);

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

    private void SetFlags(ISourceComplexTypeCollection<SourceObjectType> sources)
    {
        if (sources.Schemas.Length > 1)
        {
            _flags |= FusionTypeFlags.Shared;
        }

        foreach (var source in sources)
        {
            if (source.Lookups.Length > 0)
            {
                _flags |= FusionTypeFlags.Entity;
                break;
            }
        }
    }

    /// <summary>
    /// Creates a <see cref="ObjectTypeDefinitionNode"/> from a
    /// <see cref="FusionObjectTypeDefinition"/>.
    /// </summary>
    public new ObjectTypeDefinitionNode ToSyntaxNode()
        => (ObjectTypeDefinitionNode)base.ToSyntaxNode();
}
