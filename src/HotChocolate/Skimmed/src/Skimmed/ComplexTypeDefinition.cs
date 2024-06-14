using HotChocolate.Features;
using HotChocolate.Utilities;
using HotChocolate.Types;

namespace HotChocolate.Skimmed;

/// <summary>
/// Represents the base class for a GraphQL object type definition or an interface type definition.
/// </summary>
public abstract class ComplexTypeDefinition(string name) : INamedTypeDefinition
{
    private string _name = name.EnsureGraphQLName();
    private List<InterfaceType>? _implements;
    private DirectiveCollection? _directives;
    private FeatureCollection? _features;

    /// <inheritdoc />
    public abstract TypeKind Kind { get; }

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
    /// Gets the interfaces that are implemented by this type.
    /// </summary>
    public IList<InterfaceType> Implements => _implements ??= [];

    /// <summary>
    /// Gets the fields of this type.
    /// </summary>
    /// <value>
    /// The fields of this type.
    /// </value>
    public OutputFieldCollection Fields { get; } = [];

    /// <inheritdoc />
    public IFeatureCollection Features => _features ??= new FeatureCollection();

    /// <inheritdoc />
    public bool Equals(ITypeDefinition? other) => Equals(other, TypeComparison.Reference);

    /// <inheritdoc />
    public abstract bool Equals(ITypeDefinition? other, TypeComparison comparison);
}
