using HotChocolate.Types;
using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

/// <summary>
/// Represents a GraphQL interface type definition.
/// </summary>
public  class InterfaceTypeDefinition(string name)
    : ComplexTypeDefinition(name)
    , INamedTypeSystemMemberDefinition<InterfaceTypeDefinition>
{
    /// <inheritdoc />
    public override TypeKind Kind => TypeKind.Interface;

    /// <inheritdoc />
    public override bool Equals(ITypeDefinition? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }

        return other is InterfaceTypeDefinition otherInterface
            && otherInterface.Name.Equals(Name, StringComparison.Ordinal);
    }

    /// <summary>
    /// Gets the string representation of this instance.
    /// </summary>
    /// <returns>
    ///  The string representation of this instance.
    /// </returns>
    public override string ToString()
        => RewriteInterfaceType(this).ToString(true);

    /// <summary>
    /// Creates a new instance of <see cref="InterfaceTypeDefinition"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the interface type definition.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="InterfaceTypeDefinition"/>.
    /// </returns>
    public static InterfaceTypeDefinition Create(string name) => new(name);
}
