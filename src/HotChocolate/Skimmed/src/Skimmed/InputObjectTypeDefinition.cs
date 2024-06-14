using HotChocolate.Features;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

/// <summary>
/// Represents a GraphQL input object type definition.
/// </summary>
public sealed class InputObjectTypeDefinition(string name)
    : INamedTypeDefinition
    , INamedTypeSystemMemberDefinition<InputObjectTypeDefinition>
{
    private string _name = name.EnsureGraphQLName();
    private DirectiveCollection? _directives;
    private FeatureCollection? _features;

    /// <inheritdoc />
    public TypeKind Kind => TypeKind.InputObject;

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
    /// Gets the fields of this input object type definition.
    /// </summary>
    /// <value>
    /// The fields of this input object type definition.
    /// </value>
    public InputFieldDefinitionCollection Fields { get; } = [];

    /// <inheritdoc />
    public IFeatureCollection Features => _features ??= new FeatureCollection();

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

        return other is InputObjectTypeDefinition otherInput && otherInput.Name.Equals(Name, StringComparison.Ordinal);
    }

    /// <summary>
    /// Gets the string representation of this instance.
    /// </summary>
    /// <returns>
    /// The string representation of this instance.
    /// </returns>
    public override string ToString()
        => RewriteInputObjectType(this).ToString(true);

    /// <summary>
    /// Creates a new input object type definition.
    /// </summary>
    /// <param name="name">
    /// The name of the input object type definition.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="InputObjectTypeDefinition"/>.
    /// </returns>
    public static InputObjectTypeDefinition Create(string name) => new(name);
}
