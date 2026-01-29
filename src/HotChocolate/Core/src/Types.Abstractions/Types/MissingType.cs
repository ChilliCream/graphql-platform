using HotChocolate.Features;
using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// <para>
/// Represents a missing GraphQL type definition.
/// </para>
/// <para>
/// When a GraphQL schema document is parsed that is incomplete
/// the parser will create missing type definitions to plug the holes.
/// </para>
/// <para>
/// These can be later replaced by the actual type definitions.
/// </para>
/// </summary>
public sealed class MissingType : IInputTypeDefinition, IOutputTypeDefinition
{
    private IReadOnlyDirectiveCollection? _directives;
    private FeatureCollection? _features;

    /// <summary>
    /// <para>
    /// Represents a missing GraphQL type definition.
    /// </para>
    /// <para>
    /// When a GraphQL schema document is parsed that is incomplete
    /// the parser will create missing type definitions to plug the holes.
    /// </para>
    /// <para>
    /// These can be later replaced by the actual type definitions.
    /// </para>
    /// </summary>
    public MissingType(string name)
    {
        Name = name;
    }

    /// <inheritdoc />
    public TypeKind Kind => TypeKind.Scalar;

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string? Description { get; }

    /// <inheritdoc />
    public SchemaCoordinate Coordinate => new(Name, ofDirective: false);

    /// <inheritdoc />
    public Type RuntimeType => typeof(object);

    /// <summary>
    /// Gets the directives annotated to this type.
    /// </summary>
    public IReadOnlyDirectiveCollection Directives
        => _directives ??= EmptyCollections.Directives;

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives
        => _directives ?? EmptyCollections.Directives;

    /// <inheritdoc />
    public IFeatureCollection Features
        => _features ??= new FeatureCollection();

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

        return other is MissingType otherMissing
            && otherMissing.Name.Equals(Name, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public bool IsAssignableFrom(ITypeDefinition type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type.Kind == TypeKind.Scalar)
        {
            return Equals(type, TypeComparison.Reference);
        }

        return false;
    }

    /// <summary>
    /// Creates a <see cref="NamedTypeNode"/> from a <see cref="MissingType"/>.
    /// </summary>
    public NamedTypeNode ToSyntaxNode() => new(Name);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode() => ToSyntaxNode();
}
