using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

/// <summary>
/// Represents a GraphQL input field definition.
/// </summary>
public sealed class InputFieldDefinition(string name, ITypeDefinition? type = null)
    : IFieldDefinition
    , INamedTypeSystemMemberDefinition<InputFieldDefinition>
    , ISealable
{
    private ITypeDefinition _type = type ?? NotSetTypeDefinition.Default;
    private string _name = name.EnsureGraphQLName();
    private string? _description;
    private bool _isDeprecated;
    private string? _deprecationReason;
    private IDirectiveCollection? _directives;
    private IFeatureCollection? _features;
    private IValueNode? _defaultValue;
    private bool _isReadOnly;

    /// <inheritdoc cref="IFieldDefinition.Name" />
    public string Name
    {
        get => _name;
        set
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException(
                    "The field is sealed and cannot be modified.");
            }

            _name = value.EnsureGraphQLName();
        }
    }

    /// <inheritdoc cref="IFieldDefinition.Description" />
    public string? Description
    {
        get => _description;
        set
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException(
                    "The field is sealed and cannot be modified.");
            }

            _description = value;
        }
    }

    /// <summary>
    /// Gets or sets the default value for this input field.
    /// </summary>
    /// <value></value>
    public IValueNode? DefaultValue
    {
        get => _defaultValue;
        set
        {
            if(_isReadOnly)
            {
                throw new NotSupportedException(
                    "The field is sealed and cannot be modified.");
            }

            _defaultValue = value;
        }
    }

    /// <inheritdoc cref="IFieldDefinition.IsDeprecated" />
    public bool IsDeprecated
    {
        get => _isDeprecated;
        set
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException(
                    "The field is sealed and cannot be modified.");
            }

            _isDeprecated = value;

            if (!value)
            {
                DeprecationReason = null;
            }
        }
    }

    /// <inheritdoc cref="IFieldDefinition.DeprecationReason" />
    public string? DeprecationReason
    {
        get => _deprecationReason;
        set
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException(
                    "The field is sealed and cannot be modified.");
            }

            _deprecationReason = value;

            if (!string.IsNullOrEmpty(value))
            {
                _isDeprecated = true;
            }
        }
    }

    /// <inheritdoc />
    public IDirectiveCollection Directives
        => _directives ??= new DirectiveCollection();

    public ITypeDefinition Type
    {
        get => _type;
        set
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException(
                    "The field is sealed and cannot be modified.");
            }

            _type = value.ExpectInputType();
        }
    }

    /// <inheritdoc />
    public IFeatureCollection Features
        => _features ??= new FeatureCollection();

    /// <inheritdoc />
    public bool IsReadOnly => _isReadOnly;

    /// <summary>
    /// Seals this type makes it read-only.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    internal void Seal()
    {
        if (_isReadOnly)
        {
            return;
        }

        if(_type is NotSetTypeDefinition or MissingTypeDefinition)
        {
            throw new InvalidOperationException(
                "An input field must have a type.");
        }

        _directives = _directives is null
            ? ReadOnlyDirectiveCollection.Empty
            : ReadOnlyDirectiveCollection.From(_directives);

        _features = _features is null
            ? EmptyFeatureCollection.Default
            : _features.ToReadOnly();

        _isReadOnly = true;
    }

    void ISealable.Seal() => Seal();

    /// <summary>
    /// Gets a string that represents the current object.
    /// </summary>
    /// <returns>
    /// A string that represents the current object.
    /// </returns>
    public override string ToString()
        => RewriteInputField(this).ToString(true);

    /// <summary>
    /// Creates a new instance of <see cref="InputFieldDefinition"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the input field.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="InputFieldDefinition"/>.
    /// </returns>
    public static InputFieldDefinition Create(string name) => new(name);
}
