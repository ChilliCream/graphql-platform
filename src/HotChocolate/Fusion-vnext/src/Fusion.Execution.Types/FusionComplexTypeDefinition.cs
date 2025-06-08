using HotChocolate.Features;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Language;
using HotChocolate.Serialization;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

/// <summary>
/// Represents the base class for a GraphQL object type definition or an interface type definition.
/// </summary>
public abstract class FusionComplexTypeDefinition : IComplexTypeDefinition
{
    private FusionDirectiveCollection _directives = null!;
    private FusionInterfaceTypeDefinitionCollection _implements = null!;
    private bool _completed;

    protected FusionComplexTypeDefinition(
        string name,
        string? description,
        FusionOutputFieldDefinitionCollection fieldsDefinition)
    {
        Name = name;
        Description = description;
        Fields = fieldsDefinition;
    }

    public abstract TypeKind Kind { get; }

    public abstract bool IsEntity { get; }

    public string Name { get; }

    public string? Description { get; }

    public SchemaCoordinate Coordinate => new(Name, ofDirective: false);

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
    /// Gets the source type definition of this type.
    /// </summary>
    /// <value>
    /// The source type definition of this type.
    /// </value>
    public ISourceComplexTypeCollection<ISourceComplexType> Sources
    {
        get;
        private protected set
        {
            ThrowHelper.EnsureNotSealed(_completed);

            field = value;
        }
    } = null!;

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
    ///  The string representation of this instance.
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
