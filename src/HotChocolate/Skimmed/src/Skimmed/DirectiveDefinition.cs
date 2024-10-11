using HotChocolate.Features;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

/// <summary>
/// Represents a GraphQL directive definition.
/// </summary>
public class DirectiveDefinition(string name)
    : INamedTypeSystemMemberDefinition<DirectiveDefinition>
    , IDescriptionProvider
    , IFeatureProvider
    , ISealable
{
    private string _name = name.EnsureGraphQLName();
    private IInputFieldDefinitionCollection? _arguments;
    private IFeatureCollection? _features;
    private string? _description;
    private bool _isSpecDirective;
    private bool _isRepeatable;
    private DirectiveLocation _locations;
    private bool _isReadOnly;

    /// <summary>
    /// Gets or sets the name of the directive.
    /// </summary>
    /// <value>
    /// The name of the directive.
    /// </value>
    public string Name
    {
        get => _name.EnsureGraphQLName();
        set
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException(
                    "The directive definition is sealed and cannot be modified.");
            }

            _name = value;
        }
    }

    /// <summary>
    /// Gets or sets the description of the directive.
    /// </summary>
    /// <value>
    /// The description of the directive.
    /// </value>
    public string? Description
    {
        get => _description;
        set
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException(
                    "The directive definition is sealed and cannot be modified.");
            }

            _description = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this directive type is a spec directive.
    /// </summary>
    public bool IsSpecDirective
    {
        get => _isSpecDirective;
        set
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException(
                    "The directive is sealed and cannot be modified.");
            }

            _isSpecDirective = value;
        }
    }

    /// <summary>
    /// Defines if this directive is repeatable and can be applied multiple times.
    /// </summary>
    public bool IsRepeatable
    {
        get => _isRepeatable;
        set
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException(
                    "The directive definition is sealed and cannot be modified.");
            }

            _isRepeatable = value;
        }
    }

    /// <summary>
    /// Gets the arguments that are defined on this directive.
    /// </summary>
    public IInputFieldDefinitionCollection Arguments
        => _arguments ??= new InputFieldDefinitionCollection();

    /// <summary>
    /// Gets the locations where this directive can be applied.
    /// </summary>
    /// <value>
    /// The locations where this directive can be applied.
    /// </value>
    public DirectiveLocation Locations
    {
        get => _locations;
        set
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException(
                    "The directive definition is sealed and cannot be modified.");
            }

            _locations = value;
        }
    }

    /// <inheritdoc />
    public IFeatureCollection Features
        => _features ??= new FeatureCollection();

    /// <inheritdoc />
    public bool IsReadOnly => _isReadOnly;

    /// <summary>
    /// Seals this value and makes it read-only.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    internal void Seal()
    {
        if (_isReadOnly)
        {
            return;
        }

        _arguments = _arguments is null
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
    /// Gets a string that represents the current object.
    /// </summary>
    /// <returns>
    /// A string that represents the current object.
    /// </returns>
    public override string ToString()
        => RewriteDirectiveType(this).ToString(true);

    /// <summary>
    /// Creates a new directive definition.
    /// </summary>
    /// <param name="name">
    /// The name of the directive.
    /// </param>
    /// <returns>
    /// Returns a new directive definition.
    /// </returns>
    public static DirectiveDefinition Create(string name)
        => new(name);
}
