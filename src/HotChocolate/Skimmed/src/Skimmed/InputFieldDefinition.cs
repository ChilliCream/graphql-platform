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
{
    private ITypeDefinition _type = type ?? NotSetType.Default;
    private string _name = name.EnsureGraphQLName();
    private bool _isDeprecated;
    private string? _deprecationReason;
    private DirectiveCollection? _directives;
    private FeatureCollection? _features;

    /// <inheritdoc />
    public string Name
    {
        get => _name;
        set => _name = value.EnsureGraphQLName();
    }

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the default value for this input field.
    /// </summary>
    /// <value></value>
    public IValueNode? DefaultValue { get; set; }

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

    public ITypeDefinition Type
    {
        get => _type;
        set => _type = value.ExpectInputType();
    }
    /// <inheritdoc />
    public IFeatureCollection Features => _features ??= new FeatureCollection();

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
