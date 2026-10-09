using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.Types.Mutable;

/// <summary>
/// Represents a GraphQL object type definition.
/// </summary>
public class MutableObjectTypeDefinition(string name)
    : MutableComplexTypeDefinition(name)
    , INamedTypeSystemMemberDefinition<MutableObjectTypeDefinition>
    , IObjectTypeDefinition
{
    /// <inheritdoc />
    public override TypeKind Kind => TypeKind.Object;

    /// <inheritdoc cref="IDeprecationProvider.IsDeprecated" />
    [MemberNotNullWhen(true, nameof(DeprecationReason))]
    public bool IsDeprecated => DeprecationReason is not null;

    /// <summary>
    /// Gets or sets the deprecation reason of this type, or <c>null</c> if this type
    /// is not deprecated. Setting an empty or white-space value is equivalent to setting <c>null</c>.
    /// </summary>
    public string? DeprecationReason
    {
        get;
        set => field = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>
    /// Creates a <see cref="ObjectTypeDefinitionNode"/> from a
    /// <see cref="MutableObjectTypeDefinition"/>.
    /// </summary>
    public new ObjectTypeDefinitionNode ToSyntaxNode()
        => (ObjectTypeDefinitionNode)base.ToSyntaxNode();

    /// <inheritdoc />
    public override bool Equals(IType? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }

        return other is MutableObjectTypeDefinition otherObject
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
    /// Creates a new instance of <see cref="MutableObjectTypeDefinition"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the object type definition.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="MutableObjectTypeDefinition"/>.
    /// </returns>
    public static MutableObjectTypeDefinition Create(string name) => new(name);
}
