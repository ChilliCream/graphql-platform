using HotChocolate.Features;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Metadata;
using HotChocolate.Language;
using HotChocolate.Serialization;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Types;

/// <summary>
/// Represents the base class for a GraphQL object type definition or an interface type definition.
/// </summary>
public abstract class FusionComplexTypeDefinition : IComplexTypeDefinition, IFusionTypeDefinition
{
    private FusionDirectiveCollection _directives = null!;
    private FusionInterfaceTypeDefinitionCollection _implements = null!;
    private bool _completed;

    protected FusionComplexTypeDefinition(
        string name,
        string? description,
        bool isInaccessible,
        FusionOutputFieldDefinitionCollection fieldsDefinition)
    {
        name.EnsureGraphQLName();
        ArgumentNullException.ThrowIfNull(fieldsDefinition);

        Name = name;
        Description = description;
        IsInaccessible = isInaccessible;
        Fields = fieldsDefinition;
    }

    /// <summary>
    /// Gets the kind of this type.
    /// </summary>
    public abstract TypeKind Kind { get; }

    /// <summary>
    /// Gets the name of this type.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the description of this type.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the schema coordinate of this type.
    /// </summary>
    public SchemaCoordinate Coordinate => new(Name, ofDirective: false);

    Type IRuntimeTypeProvider.RuntimeType => typeof(object);

    /// <summary>
    /// Gets a value indicating whether this type is marked as inaccessible.
    /// </summary>
    public bool IsInaccessible { get; }

    /// <summary>
    /// Gets the directives applied to this type.
    /// </summary>
    public FusionDirectiveCollection Directives
    {
        get => _directives;
        private protected set
        {
            ThrowHelper.EnsureNotSealed(_completed);

            _directives = value;
        }
    }

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives
        => _directives;

    /// <summary>
    /// Gets the interfaces that are implemented by this type.
    /// </summary>
    public FusionInterfaceTypeDefinitionCollection Implements
    {
        get => _implements;
        private protected set
        {
            ThrowHelper.EnsureNotSealed(_completed);

            _implements = value;
        }
    }

    IReadOnlyInterfaceTypeDefinitionCollection IComplexTypeDefinition.Implements
        => _implements;

    /// <summary>
    /// Gets the fields of this type.
    /// </summary>
    /// <value>
    /// The fields of this type.
    /// </value>
    public FusionOutputFieldDefinitionCollection Fields { get; }

    IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition> IComplexTypeDefinition.Fields
        => Fields;

    /// <summary>
    /// Gets metadata about this complex type in its source schemas.
    /// Each entry in the collection provides information about this complex type
    /// that is specific to the source schemas the type was composed of.
    /// </summary>
    public ISourceComplexTypeCollection<ISourceComplexType> Sources
    {
        get;
        private protected set
        {
            ThrowHelper.EnsureNotSealed(_completed);

            field = value;
        }
    } = null!;

    /// <summary>
    /// Gets the feature collection associated with this type.
    /// </summary>
    public IFeatureCollection Features
    {
        get;
        private protected set
        {
            if (_completed)
            {
                throw new NotSupportedException(
                    "The type definition is sealed and cannot be modified.");
            }

            field = value;
        }
    } = FeatureCollection.Empty;

    private protected void Complete()
    {
        ThrowHelper.EnsureNotSealed(_completed);

        _completed = true;
    }

    /// <inheritdoc />
    public bool Equals(IType? other) => Equals(other, TypeComparison.Reference);

    /// <inheritdoc />
    public abstract bool Equals(IType? other, TypeComparison comparison);

    /// <inheritdoc />
    public abstract bool IsAssignableFrom(ITypeDefinition type);

    /// <inheritdoc />
    public bool IsImplementing(string typeName)
        => Implements.ContainsName(typeName);

    /// <inheritdoc />
    public bool IsImplementing(IInterfaceTypeDefinition interfaceType)
        => Implements.ContainsName(interfaceType.Name);

    /// <summary>
    /// Gets the string representation of this instance.
    /// </summary>
    /// <returns>
    /// The string representation of this instance.
    /// </returns>
    public override string ToString()
        => ToSyntaxNode().ToString(true);

    /// <summary>
    /// Creates a <see cref="ComplexTypeDefinitionNodeBase"/> from a
    /// <see cref="FusionComplexTypeDefinition"/>.
    /// </summary>
    public ComplexTypeDefinitionNodeBase ToSyntaxNode() => this switch
    {
        FusionInterfaceTypeDefinition i => SchemaDebugFormatter.Format(i),
        FusionObjectTypeDefinition o => SchemaDebugFormatter.Format(o),
        _ => throw new ArgumentOutOfRangeException()
    };

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode() => ToSyntaxNode();
}
