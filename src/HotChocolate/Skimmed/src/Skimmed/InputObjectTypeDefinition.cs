using HotChocolate.Features;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

/// <summary>
/// Represents a GraphQL input object type definition.
/// </summary>
public class InputObjectTypeDefinition(string name)
    : INamedTypeDefinition
    , INamedTypeSystemMemberDefinition<InputObjectTypeDefinition>
    , ISealable
{
    private string _name = name.EnsureGraphQLName();
    private IDirectiveCollection? _directives;
    private IInputFieldDefinitionCollection _fields = new InputFieldDefinitionCollection();
    private IFeatureCollection? _features;
    private string? _description;
    private bool _isReadOnly;

    /// <inheritdoc />
    public TypeKind Kind => TypeKind.InputObject;

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
    /// Gets the fields of this input object type definition.
    /// </summary>
    /// <value>
    /// The fields of this input object type definition.
    /// </value>
    public IInputFieldDefinitionCollection Fields
        => _fields;

    /// <inheritdoc />
    public IFeatureCollection Features
        => _features ??= new FeatureCollection();

    /// <inheritdoc />
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
                "An input object type must have at least one value.");
        }

        _directives = _directives is null
            ? ReadOnlyDirectiveCollection.Empty
            : ReadOnlyDirectiveCollection.From(_directives);

        _fields = ReadOnlyInputFieldDefinitionCollection.From(_fields);

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
    public bool Equals(ITypeDefinition? other)
        => Equals(other, TypeComparison.Reference);

    /// <inheritdoc />
    public bool Equals(ITypeDefinition? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }

        return other is InputObjectTypeDefinition otherInput
            && otherInput.Name.Equals(Name, StringComparison.Ordinal);
    }

    /// <summary>
    /// Gets the string representation of this instance.
    /// </summary>
    /// <returns>
    /// The string representation of this instance.
    /// </returns>
    public override string ToString()
        => RewriteInputObjectType(this).ToString(true);

    /// <summary>
    /// Creates a new input object type definition.
    /// </summary>
    /// <param name="name">
    /// The name of the input object type definition.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="InputObjectTypeDefinition"/>.
    /// </returns>
    public static InputObjectTypeDefinition Create(string name) => new(name);
}
