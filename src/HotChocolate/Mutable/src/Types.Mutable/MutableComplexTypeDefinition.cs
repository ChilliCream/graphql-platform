using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Serialization;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Mutable;

/// <summary>
/// Represents the base class for a GraphQL object type definition or an interface type definition.
/// </summary>
public abstract class MutableComplexTypeDefinition
    : IComplexTypeDefinition
    , IMutableTypeDefinition
    , IFeatureProvider
{
    private DirectiveCollection? _directives;
    private InterfaceTypeDefinitionCollection? _implements;

    /// <summary>
    /// Represents the base class for a GraphQL object type definition or an interface type definition.
    /// </summary>
    protected MutableComplexTypeDefinition(string name)
    {
        Name = name;
        Fields = new OutputFieldDefinitionCollection(this);
    }

    /// <inheritdoc />
    public abstract TypeKind Kind { get; }

    /// <inheritdoc cref="IMutableTypeDefinition.Name" />
    public string Name
    {
        get;
        set => field = value.EnsureGraphQLName();
    }

    /// <inheritdoc cref="IMutableTypeDefinition.Description" />
    public string? Description { get; set; }

    /// <inheritdoc cref="ISchemaCoordinateProvider.Coordinate" />
    public SchemaCoordinate Coordinate => new(Name, ofDirective: false);

    Type IRuntimeTypeProvider.RuntimeType => typeof(object);

    /// <summary>
    /// Gets the interfaces that are implemented by this type.
    /// </summary>
    public InterfaceTypeDefinitionCollection Implements
        => _implements ??= [];

    IReadOnlyInterfaceTypeDefinitionCollection IComplexTypeDefinition.Implements
        => _implements ?? EmptyCollections.InterfaceTypeDefinitions;

    public DirectiveCollection Directives
        => _directives ??= [];

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives
        => _directives ?? EmptyCollections.Directives;

    /// <summary>
    /// Gets the fields of this type.
    /// </summary>
    /// <value>
    /// The fields of this type.
    /// </value>
    public OutputFieldDefinitionCollection Fields { get; }

    IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition> IComplexTypeDefinition.Fields
        => Fields;

    /// <inheritdoc />
    [field: AllowNull, MaybeNull]
    public IFeatureCollection Features
        => field ??= new FeatureCollection();

    /// <inheritdoc cref="IMutableTypeDefinition.IsIntrospectionType" />
    public bool IsIntrospectionType { get; set; }

    /// <inheritdoc />
    public bool Equals(IType? other) => Equals(other, TypeComparison.Reference);

    /// <inheritdoc />
    public abstract bool Equals(IType? other, TypeComparison comparison);

    /// <inheritdoc />
    public abstract bool IsAssignableFrom(ITypeDefinition type);

    /// <inheritdoc />
    public bool IsImplementing(string typeName)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeName);
        return Implements.ContainsName(typeName);
    }

    /// <inheritdoc />
    public bool IsImplementing(IInterfaceTypeDefinition interfaceType)
    {
        ArgumentNullException.ThrowIfNull(interfaceType);
        return Implements.Contains(interfaceType);
    }

    /// <summary>
    /// Gets the string representation of this instance.
    /// </summary>
    /// <returns>
    ///  The string representation of this instance.
    /// </returns>
    public override string ToString()
        => ToSyntaxNode().ToString(true);

    /// <summary>
    /// Creates a <see cref="ComplexTypeDefinitionNodeBase"/> from a
    /// <see cref="MutableComplexTypeDefinition"/>.
    /// </summary>
    public ComplexTypeDefinitionNodeBase ToSyntaxNode()
        => this switch
        {
            MutableInterfaceTypeDefinition i => SchemaDebugFormatter.Format(i),
            MutableObjectTypeDefinition o => SchemaDebugFormatter.Format(o),
            _ => throw new ArgumentOutOfRangeException()
        };

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode() => ToSyntaxNode();
}
