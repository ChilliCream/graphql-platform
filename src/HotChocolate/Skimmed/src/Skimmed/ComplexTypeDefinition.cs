using HotChocolate.Features;
using HotChocolate.Utilities;
using HotChocolate.Types;

namespace HotChocolate.Skimmed;

/// <summary>
/// Represents the base class for a GraphQL object type definition or an interface type definition.
/// </summary>
public abstract class ComplexTypeDefinition(string name)
    : INamedTypeDefinition
    , ISealable
{
    private string _name = name.EnsureGraphQLName();
    private string? _description;
    private IDirectiveCollection? _directives;
    private IInterfaceTypeDefinitionCollection? _implements;
    private IOutputFieldDefinitionCollection _fields = new OutputFieldDefinitionCollection();
    private IFeatureCollection? _features;
    private bool _isReadOnly;

    /// <inheritdoc />
    public abstract TypeKind Kind { get; }

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
    /// Gets the interfaces that are implemented by this type.
    /// </summary>
    public IInterfaceTypeDefinitionCollection Implements
        => _implements ??= new InterfaceTypeDefinitionCollection();

    /// <summary>
    /// Gets the fields of this type.
    /// </summary>
    /// <value>
    /// The fields of this type.
    /// </value>
    public IOutputFieldDefinitionCollection Fields
        => _fields;

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

        foreach (var field in _fields)
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
}
