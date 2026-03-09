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
    , IMutableTypeDefinition
    , IFeatureProvider
{
    private DirectiveCollection? _directives;

    /// <summary>
    /// Represents a GraphQL input object type definition.
    /// </summary>
    public MutableInputObjectTypeDefinition(string name)
    {
        Name = name.EnsureGraphQLName();
        Fields = new InputFieldDefinitionCollection(this);
    }

    /// <inheritdoc />
    public TypeKind Kind => TypeKind.InputObject;

    /// <inheritdoc cref="IMutableTypeDefinition.Name" />
    public string Name
    {
        get;
        set => field = value.EnsureGraphQLName();
    }

    /// <inheritdoc cref="IMutableTypeDefinition.Description" />
    public string? Description { get; set; }

    /// <inheritdoc />
    public SchemaCoordinate Coordinate
        => new(Name, ofDirective: false);

    Type IRuntimeTypeProvider.RuntimeType => typeof(object);

    /// <inheritdoc />
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
    public InputFieldDefinitionCollection Fields { get; }

    IReadOnlyFieldDefinitionCollection<IInputValueDefinition> IInputObjectTypeDefinition.Fields
        => Fields;

    /// <inheritdoc cref="IMutableTypeDefinition.IsIntrospectionType" />
    public bool IsIntrospectionType { get; set; }

    /// <inheritdoc />
    public bool IsOneOf { get; set; }

    /// <inheritdoc />
    [field: AllowNull, MaybeNull]
    public IFeatureCollection Features
        => field ??= new FeatureCollection();

    /// <inheritdoc />
    public bool Equals(IType? other)
        => Equals(other, TypeComparison.Reference);

    /// <inheritdoc />
    public bool Equals(IType? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }

        return other is MutableInputObjectTypeDefinition otherInput
            && otherInput.Name.Equals(Name, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public bool IsAssignableFrom(ITypeDefinition type)
    {
        ArgumentNullException.ThrowIfNull(type);

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
    /// <see cref="MutableInputObjectTypeDefinition"/>.
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
    /// Returns a new instance of <see cref="MutableInputObjectTypeDefinition"/>.
    /// </returns>
    public static MutableInputObjectTypeDefinition Create(string name) => new(name);
}
