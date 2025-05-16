using HotChocolate.Language;

namespace HotChocolate.Types.Mutable;

/// <summary>
/// Represents a GraphQL interface type definition.
/// </summary>
public class MutableInterfaceTypeDefinition(string name)
    : MutableComplexTypeDefinition(name)
    , INamedTypeSystemMemberDefinition<MutableInterfaceTypeDefinition>
    , IInterfaceTypeDefinition
{
    /// <inheritdoc />
    public override TypeKind Kind => TypeKind.Interface;

    /// <inheritdoc />
    public SchemaCoordinate Coordinate => new(Name, ofDirective: false);

    /// <summary>
    /// Creates a <see cref="InterfaceTypeDefinitionNode"/> from a
    /// <see cref="MutableInterfaceTypeDefinition"/>.
    /// </summary>
    public new InterfaceTypeDefinitionNode ToSyntaxNode()
        => (InterfaceTypeDefinitionNode)base.ToSyntaxNode();

    /// <inheritdoc />
    public override bool Equals(IType? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }

        return other is MutableInterfaceTypeDefinition otherInterface
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
    /// Creates a new instance of <see cref="MutableInterfaceTypeDefinition"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the interface type definition.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="MutableInterfaceTypeDefinition"/>.
    /// </returns>
    public static MutableInterfaceTypeDefinition Create(string name) => new(name);
}
