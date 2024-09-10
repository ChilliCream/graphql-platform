using HotChocolate.Features;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

/// <summary>
/// Represents a GraphQL union type definition.
/// </summary>
public class UnionTypeDefinition(string name)
    : INamedTypeDefinition
    , INamedTypeSystemMemberDefinition<UnionTypeDefinition>
    , ISealable
{
    private string _name = name.EnsureGraphQLName();
    private string? _description;
    private IDirectiveCollection? _directives;
    private IObjectTypeDefinitionCollection? _types;
    private IFeatureCollection? _features;
    private bool _isReadOnly;

    /// <inheritdoc />
    public TypeKind Kind => TypeKind.Union;

    /// <inheritdoc cref="INamedTypeDefinition.Name" />
    public string Name
    {
        get => _name;
        set
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException(
                    "The type is sealed and cannot be modified.");
            }

            _name = value.EnsureGraphQLName();
        }
    }

    /// <inheritdoc cref="INamedTypeDefinition.Description" />
    public string? Description
    {
        get => _description;
        set
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException(
                    "The type is sealed and cannot be modified.");
            }

            _description = value;
        }
    }

    /// <inheritdoc />
    public IDirectiveCollection Directives
        => _directives ??= new DirectiveCollection();

    /// <summary>
    /// Gets the types that are part of this union.
    /// </summary>
    public IObjectTypeDefinitionCollection Types
        => _types ??= new ObjectTypeDefinitionCollection();

    /// <inheritdoc />
    public IFeatureCollection Features
        => _features ??= new FeatureCollection();

    public bool IsReadOnly => _isReadOnly;

    /// <summary>
    /// Seals this type and makes it read-only.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    protected internal void Seal()
    {
        if (_isReadOnly)
        {
            return;
        }

        _directives = _directives is null
            ? ReadOnlyDirectiveCollection.Empty
            : ReadOnlyDirectiveCollection.From(_directives);

        _types = _types is null
            ? ReadOnlyObjectTypeDefinitionCollection.Empty
            : ReadOnlyObjectTypeDefinitionCollection.From(_types);

        _features = _features is null
            ? EmptyFeatureCollection.Default
            : _features.ToReadOnly();

        _isReadOnly = true;
    }

    void ISealable.Seal() => Seal();

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

        return other is UnionTypeDefinition otherUnion
            && otherUnion.Name.Equals(Name, StringComparison.Ordinal);
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
