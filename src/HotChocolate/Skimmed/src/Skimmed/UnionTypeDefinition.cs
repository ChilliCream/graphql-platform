using HotChocolate.Features;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

/// <summary>
/// Represents a GraphQL union type definition.
/// </summary>
public sealed class UnionTypeDefinition(string name)
    : INamedTypeDefinition
    , INamedTypeSystemMemberDefinition<UnionTypeDefinition>
{
    private string _name = name.EnsureGraphQLName();
    private DirectiveCollection? _directives;
    private FeatureCollection? _features;

    /// <inheritdoc />
    public TypeKind Kind => TypeKind.Union;

    /// <inheritdoc />
    public string Name
    {
        get => _name;
        set => _name = value.EnsureGraphQLName();
    }

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public DirectiveCollection Directives => _directives ??= [];

    public IList<ObjectTypeDefinition> Types { get; } = [];

    /// <inheritdoc />
    public IFeatureCollection Features => _features ??= new FeatureCollection();

    /// <summary>
    /// Get the string representation of the union type definition.
    /// </summary>
    /// <returns>
    /// Returns the string representation of the union type definition.
    /// </returns>
    public override string ToString()
        => RewriteUnionType(this).ToString(true);

    /// <inheritdoc />
    public bool Equals(ITypeDefinition? other)
        => Equals(other, TypeComparison.Reference);

    /// <inheritdoc />
    public bool Equals(ITypeDefinition? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }

        return other is UnionTypeDefinition otherUnion && otherUnion.Name.Equals(Name, StringComparison.Ordinal);
    }

    /// <summary>
    /// Creates a new union type definition.
    /// </summary>
    /// <param name="name">
    /// The name of the union type.
    /// </param>
    /// <returns>
    /// The created union type definition.
    /// </returns>
    public static UnionTypeDefinition Create(string name) => new(name);
}
