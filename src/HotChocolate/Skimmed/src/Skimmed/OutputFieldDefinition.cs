using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

/// <summary>
/// Represents a GraphQL output field definition.
/// </summary>
public sealed class OutputFieldDefinition(string name, ITypeDefinition? type = null)
    : IFieldDefinition
    , INamedTypeSystemMemberDefinition<OutputFieldDefinition>
    , IReadOnlyOutputFieldDefinition
    , ISealable
{
    private string _name = name.EnsureGraphQLName();
    private string? _description;
    private bool _isDeprecated;
    private string? _deprecationReason;
    private IDirectiveCollection? _directives;
    private IFeatureCollection? _features;
    private IInputFieldDefinitionCollection? _arguments;
    private ITypeDefinition _type = type ?? NotSetTypeDefinition.Default;
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
                    "The value is sealed and cannot be modified.");
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
                    "The value is sealed and cannot be modified.");
            }

            _description = value;
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
                    "The value is sealed and cannot be modified.");
            }

            _isDeprecated = value;

            if (!value)
            {
                _deprecationReason = null;
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
                    "The value is sealed and cannot be modified.");
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

    IReadOnlyDirectiveCollection IReadOnlyFieldDefinition.Directives
        => _directives as IReadOnlyDirectiveCollection
            ?? ReadOnlyDirectiveCollection.Empty;

    /// <summary>
    /// Gets the arguments that are accepted by this field.
    /// </summary>
    public IInputFieldDefinitionCollection Arguments
        => _arguments ??= new InputFieldDefinitionCollection();

    IReadOnlyFieldDefinitionCollection<IReadOnlyInputValueDefinition> IReadOnlyOutputFieldDefinition.Arguments
        => _arguments as IReadOnlyFieldDefinitionCollection<IReadOnlyInputValueDefinition>
            ?? ReadOnlyInputFieldDefinitionCollection.Empty;

    /// <summary>
    /// Gets the type of the field.
    /// </summary>
    /// <value>
    /// The type of the field.
    /// </value>
    public ITypeDefinition Type
    {
        get => _type;
        set
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException(
                    "The value is sealed and cannot be modified.");
            }

            _type = value;
        }
    }

    IReadOnlyTypeDefinition IReadOnlyFieldDefinition.Type => Type;

    /// <inheritdoc />
    public IFeatureCollection Features
        => _features ??= new FeatureCollection();

    /// <inheritdoc />
    public bool IsReadOnly => _isReadOnly;

    /// <summary>
    /// Seals this value and makes it read-only.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    private void Seal()
    {
        if (_isReadOnly)
        {
            return;
        }

        _directives = _directives is null
            ? ReadOnlyDirectiveCollection.Empty
            : ReadOnlyDirectiveCollection.From(_directives);

        _arguments = _arguments is null || _arguments.Count == 0
            ? ReadOnlyInputFieldDefinitionCollection.Empty
            : ReadOnlyInputFieldDefinitionCollection.From(_arguments);

        _features = _features is null
            ? EmptyFeatureCollection.Default
            : _features.ToReadOnly();

        foreach (var argument in _arguments)
        {
            argument.Seal();
        }

        _isReadOnly = true;
    }

    void ISealable.Seal() => Seal();

    /// <summary>
    /// Gets the string representation of this instance.
    /// </summary>
    /// <returns>
    /// The string representation of this instance.
    /// </returns>
    public override string ToString()
        => RewriteOutputField(this).ToString(true);

    /// <summary>
    /// Creates a <see cref="FieldDefinitionNode"/> from an <see cref="OutputFieldDefinition"/>.
    /// </summary>
    public FieldDefinitionNode ToSyntaxNode() => RewriteOutputField(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode() => RewriteOutputField(this);

    /// <summary>
    /// Creates a new output field definition.
    /// </summary>
    /// <param name="name">
    /// The name of the output field definition.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="OutputFieldDefinition"/>.
    /// </returns>
    public static OutputFieldDefinition Create(string name) => new(name);
}
