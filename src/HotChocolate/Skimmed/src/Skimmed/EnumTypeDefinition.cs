using HotChocolate.Features;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

/// <summary>
/// Represents a GraphQL enum type definition.
/// </summary>
public sealed class EnumTypeDefinition(string name)
    : INamedTypeDefinition
    , INamedTypeSystemMemberDefinition<EnumTypeDefinition>
{
    private string _name = name.EnsureGraphQLName();
    private DirectiveCollection? _directives;
    private FeatureCollection? _features;
    private EnumValueCollection? _values;

    /// <inheritdoc />
    public TypeKind Kind => TypeKind.Enum;

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

    /// <summary>
    /// Gets the values of this enum type.
    /// </summary>
    /// <value>
    /// The values of this enum type.
    /// </value>
    public EnumValueCollection Values => _values ??= [];

    /// <inheritdoc />
    public IFeatureCollection Features => _features ??= new FeatureCollection();

    /// <summary>
    /// Gets the string representation of this instance.
    /// </summary>
    /// <returns>
    /// The string representation of this instance.
    /// </returns>
    public override string ToString()
        => RewriteEnumType(this).ToString(true);

    /// <inheritdoc />
    public bool Equals(ITypeDefinition? other) => Equals(other, TypeComparison.Reference);

    /// <inheritdoc />
    public bool Equals(ITypeDefinition? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }

        return other is EnumTypeDefinition otherEnum && otherEnum.Name.Equals(Name, StringComparison.Ordinal);
    }

    /// <summary>
    /// Creates a new instance of <see cref="EnumTypeDefinition"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the enum type.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="EnumTypeDefinition"/>.
    /// </returns>
    public static EnumTypeDefinition Create(string name) => new(name);
}
