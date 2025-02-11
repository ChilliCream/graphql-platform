using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Serialization;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Mutable;

/// <summary>
/// Represents the base class for a GraphQL object type definition or an interface type definition.
/// </summary>
public abstract class MutableComplexTypeDefinition(string name) : IComplexTypeDefinition
{
    private string _name = name.EnsureGraphQLName();
    private string? _description;
    private DirectiveCollection? _directives;
    private InterfaceTypeDefinitionCollection? _implements;
    private OutputFieldDefinitionCollection _fields = [];
    private IFeatureCollection? _features;
    private bool _isReadOnly;

    /// <inheritdoc />
    public abstract TypeKind Kind { get; }

    /// <inheritdoc cref="INamedTypeDefinition.Name" />
    public string Name
    {
        get => _name;
        set => _name = value.EnsureGraphQLName();
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

    public DirectiveCollection Directives
        => _directives ??= [];

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives
        => _directives ?? EmptyCollections.Directives;

    /// <summary>
    /// Gets the interfaces that are implemented by this type.
    /// </summary>
    public InterfaceTypeDefinitionCollection Implements
        => _implements ??= [];

    IReadOnlyInterfaceTypeDefinitionCollection IComplexTypeDefinition.Implements
        => _implements ?? EmptyCollections.InterfaceTypeDefinitions;

    /// <summary>
    /// Gets the fields of this type.
    /// </summary>
    /// <value>
    /// The fields of this type.
    /// </value>
    public OutputFieldDefinitionCollection Fields
        => _fields;

    IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition> IComplexType.Fields
        => _fields as IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition>
            ?? ReadOnlyOutputFieldDefinitionCollection.Empty;

    /// <inheritdoc />
    public IFeatureCollection Features
        => _features ??= new FeatureCollection();

    public bool IsReadOnly => _isReadOnly;

    /// <summary>
    /// Seals this type and makes it read-only.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    private void Seal()
    {
        if (_isReadOnly)
        {
            return;
        }

        if(_fields.Count == 0)
        {
            throw new InvalidOperationException(
                "A complex type must have at least one field.");
        }

        _directives = _directives is null
            ? ReadOnlyDirectiveCollection.Empty
            : ReadOnlyDirectiveCollection.From(_directives);

        _implements = _implements is null || _implements.Count == 0
            ? ReadOnlyInterfaceTypeDefinitionCollection.Empty
            : ReadOnlyInterfaceTypeDefinitionCollection.From(_implements);

        _fields = ReadOnlyOutputFieldDefinitionCollection.From(_fields);

        _features = _features is null
            ? EmptyFeatureCollection.Default
            : _features.ToReadOnly();

        foreach (ISealable field in _fields)
        {
            field.Seal();
        }

        _isReadOnly = true;
    }

    void ISealable.Seal() => Seal();

    /// <inheritdoc />
    public bool Equals(ITypeDefinition? other) => Equals(other, TypeComparison.Reference);

    /// <inheritdoc />
    public abstract bool Equals(ITypeDefinition? other, TypeComparison comparison);

    /// <summary>
    /// Creates a <see cref="ComplexTypeDefinitionNodeBase"/> from a
    /// <see cref="MutableComplexTypeDefinition"/>.
    /// </summary>
    public ComplexTypeDefinitionNodeBase ToSyntaxNode() => this switch
    {
        InterfaceTypeDefinition i => SchemaDebugFormatter.Format(i),
        ObjectTypeDefinition o => SchemaDebugFormatter.Format(o),
        _ => throw new ArgumentOutOfRangeException()
    };

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode() => this switch
    {
        InterfaceTypeDefinition i => SchemaDebugFormatter.Format(i),
        ObjectTypeDefinition o => SchemaDebugFormatter.Format(o),
        _ => throw new ArgumentOutOfRangeException()
    };
}
