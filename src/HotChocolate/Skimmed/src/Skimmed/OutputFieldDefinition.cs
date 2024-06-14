using HotChocolate.Features;
using HotChocolate.Utilities;
using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

/// <summary>
/// Represents a GraphQL output field definition.
/// </summary>
public sealed class OutputFieldDefinition(string name)
    : IFieldDefinition
    , INamedTypeSystemMemberDefinition<OutputFieldDefinition>
{
    private string _name = name.EnsureGraphQLName();
    private bool _isDeprecated;
    private string? _deprecationReason;
    private DirectiveCollection? _directives;
    private FeatureCollection? _features;
    private InputFieldDefinitionCollection? _arguments;

    /// <inheritdoc />
    public string Name
    {
        get => _name;
        set => _name = value.EnsureGraphQLName();
    }

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public bool IsDeprecated
    {
        get => _isDeprecated;
        set
        {
            _isDeprecated = value;

            if (!value)
            {
                DeprecationReason = null;
            }
        }
    }

    /// <inheritdoc />
    public string? DeprecationReason
    {
        get => _deprecationReason;
        set
        {
            _deprecationReason = value;

            if (!string.IsNullOrEmpty(value))
            {
                _isDeprecated = true;
            }
        }
    }

    /// <inheritdoc />
    public DirectiveCollection Directives => _directives ??= [];

    /// <summary>
    /// Gets the arguments that are accepted by this field.
    /// </summary>
    public InputFieldDefinitionCollection Arguments => _arguments ??= [];

    /// <summary>
    /// Gets the type of the field.
    /// </summary>
    /// <value>
    /// The type of the field.
    /// </value>
    public ITypeDefinition Type { get; set; } = NotSetTypeDefinition.Default;

    /// <inheritdoc />
    public IFeatureCollection Features => _features ??= new FeatureCollection();

    /// <summary>
    /// Gets the string representation of this instance.
    /// </summary>
    /// <returns>
    /// The string representation of this instance.
    /// </returns>
    public override string ToString()
        => RewriteOutputField(this).ToString(true);

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
