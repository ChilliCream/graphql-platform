using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Serialization;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Mutable;

/// <summary>
/// Represents a GraphQL input object type definition.
/// </summary>
public class MutableInputObjectTypeDefinition
    : INamedTypeSystemMemberDefinition<MutableInputObjectTypeDefinition>
    , IInputObjectTypeDefinition
    , IFeatureProvider
{
    private readonly InputFieldDefinitionCollection _fields = [];
    private DirectiveCollection? _directives;

    /// <summary>
    /// Represents a GraphQL input object type definition.
    /// </summary>
    public MutableInputObjectTypeDefinition(string name)
    {
        Name = name.EnsureGraphQLName();
    }

    /// <inheritdoc />
    public TypeKind Kind => TypeKind.InputObject;

    /// <inheritdoc cref="INamedTypeDefinition.Name" />
    public string Name
    {
        get;
        set => field = value.EnsureGraphQLName();
    }

    /// <inheritdoc cref="INamedTypeDefinition.Description" />
    public string? Description { get; set; }

    public DirectiveCollection Directives
        => _directives ??= [];

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives
        => _directives ?? EmptyCollections.Directives;

    /// <summary>
    /// Gets the fields of this input object type definition.
    /// </summary>
    /// <value>
    /// The fields of this input object type definition.
    /// </value>
    public InputFieldDefinitionCollection Fields
        => _fields;

    IReadOnlyFieldDefinitionCollection<IInputValueDefinition> IInputObjectTypeDefinition.Fields
        => _fields;

    /// <inheritdoc />
    [field: AllowNull, MaybeNull]
    public IFeatureCollection Features
        => field ??= new FeatureCollection();

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

        return other is InputObjectTypeDefinition otherInput
            && otherInput.Name.Equals(Name, StringComparison.Ordinal);
    }

    public bool IsAssignableFrom(INamedTypeDefinition type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (type.Kind == TypeKind.InputObject)
        {
            return Equals(type, TypeComparison.Reference);
        }

        return false;
    }

    /// <summary>
    /// Gets the string representation of this instance.
    /// </summary>
    /// <returns>
    /// The string representation of this instance.
    /// </returns>
    public override string ToString()
        => SchemaDebugFormatter.Format(this).ToString(true);

    /// <summary>
    /// Creates an <see cref="InputObjectTypeDefinitionNode"/> from an
    /// <see cref="InputObjectTypeDefinition"/>.
    /// </summary>
    public InputObjectTypeDefinitionNode ToSyntaxNode() => SchemaDebugFormatter.Format(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode() => SchemaDebugFormatter.Format(this);

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
