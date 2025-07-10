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
