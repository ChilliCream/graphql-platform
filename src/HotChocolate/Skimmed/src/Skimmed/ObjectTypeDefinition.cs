using HotChocolate.Types;
using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

/// <summary>
/// Represents a GraphQL object type definition.
/// </summary>
public class ObjectTypeDefinition(string name)
    : ComplexTypeDefinition(name)
    , INamedTypeSystemMemberDefinition<ObjectTypeDefinition>
{
    /// <inheritdoc />
    public override TypeKind Kind => TypeKind.Object;

    /// <summary>
    /// Gets the string representation of this instance.
    /// </summary>
    /// <returns>
    /// The string representation of this instance.
    /// </returns>
    public override string ToString()
        => RewriteObjectType(this).ToString(true);

    /// <inheritdoc />
    public override bool Equals(ITypeDefinition? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }

        return other is ObjectTypeDefinition otherObject && otherObject.Name.Equals(Name, StringComparison.Ordinal);
    }

    /// <summary>
    /// Creates a new instance of <see cref="ObjectTypeDefinition"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the object type definition.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="ObjectTypeDefinition"/>.
    /// </returns>
    public static ObjectTypeDefinition Create(string name) => new(name);
}
