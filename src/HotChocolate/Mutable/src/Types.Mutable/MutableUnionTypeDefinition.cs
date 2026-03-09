using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Serialization;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Mutable;

/// <summary>
/// Represents a GraphQL union type definition.
/// </summary>
public class MutableUnionTypeDefinition
    : INamedTypeSystemMemberDefinition<MutableUnionTypeDefinition>
    , IUnionTypeDefinition
    , IMutableTypeDefinition
    , IFeatureProvider
{
    private readonly ObjectTypeDefinitionCollection _types = [];
    private DirectiveCollection? _directives;

    public MutableUnionTypeDefinition(string name)
    {
        Name = name.EnsureGraphQLName();
    }

    /// <inheritdoc />
    public TypeKind Kind => TypeKind.Union;

    /// <inheritdoc cref="IMutableTypeDefinition.Name" />
    public string Name
    {
        get;
        set => field = value.EnsureGraphQLName();
    }

    /// <inheritdoc cref="IMutableTypeDefinition.Description" />
    public string? Description { get; set; }

    Type IRuntimeTypeProvider.RuntimeType => typeof(object);

    public DirectiveCollection Directives
        => _directives ??= [];

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives
        => _directives ?? EmptyCollections.Directives;

    /// <summary>
    /// Gets the types that are part of this union.
    /// </summary>
    public ObjectTypeDefinitionCollection Types
        => _types;

    IReadOnlyObjectTypeDefinitionCollection IUnionTypeDefinition.Types
        => _types;

    /// <inheritdoc />
    [field: AllowNull, MaybeNull]
    public IFeatureCollection Features
        => field ??= new FeatureCollection();

    /// <inheritdoc />
    public SchemaCoordinate Coordinate
        => new(Name, ofDirective: false);

    /// <inheritdoc cref="IMutableTypeDefinition.IsIntrospectionType" />
    public bool IsIntrospectionType { get; set; }

    /// <summary>
    /// Get the string representation of the union type definition.
    /// </summary>
    /// <returns>
    /// Returns the string representation of the union type definition.
    /// </returns>
    public override string ToString()
        => SchemaDebugFormatter.Format(this).ToString(true);

    /// <summary>
    /// Creates a <see cref="UnionTypeDefinitionNode"/> from a <see cref="MutableUnionTypeDefinition"/>.
    /// </summary>
    public UnionTypeDefinitionNode ToSyntaxNode()
        => SchemaDebugFormatter.Format(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode()
        => SchemaDebugFormatter.Format(this);

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

        return other is MutableUnionTypeDefinition otherUnion
            && otherUnion.Name.Equals(Name, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public bool IsAssignableFrom(ITypeDefinition type)
    {
        ArgumentNullException.ThrowIfNull(type);

        switch (type.Kind)
        {
            case TypeKind.Union:
                return ReferenceEquals(type, this);

            case TypeKind.Object:
                return _types.ContainsName(((IObjectTypeDefinition)type).Name);

            default:
                return false;
        }
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
    public static MutableUnionTypeDefinition Create(string name) => new(name);
}
