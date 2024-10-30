using HotChocolate.Features;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Skimmed;

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
public sealed class MissingTypeDefinition(string name)
    : INamedTypeDefinition
{
    private string _name = name.EnsureGraphQLName();
    private DirectiveCollection? _directives;
    private FeatureCollection? _features;

    /// <inheritdoc />
    public TypeKind Kind => TypeKind.Scalar;

    /// <inheritdoc cref="INamedTypeDefinition.Name" />
    public string Name
    {
        get => _name;
        set => _name = value.EnsureGraphQLName();
    }

    /// <inheritdoc cref="INamedTypeDefinition.Description" />
    public string? Description { get; set; }

    /// <inheritdoc />
    public IDirectiveCollection Directives => _directives ??= [];

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

        return other is MissingTypeDefinition otherMissing
            && otherMissing.Name.Equals(Name, StringComparison.Ordinal);
    }
}
