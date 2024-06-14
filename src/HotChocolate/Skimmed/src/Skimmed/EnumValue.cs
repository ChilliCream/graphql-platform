using HotChocolate.Features;
using HotChocolate.Utilities;
using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

/// <summary>
/// Represents a GraphQL enum value.
/// </summary>
public sealed class EnumValue(string name)
    : INamedTypeSystemMemberDefinition<EnumValue>
    , IDirectivesProvider
    , IFeatureProvider
    , IDescriptionProvider
    , IDeprecationProvider
{
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

    /// <inheritdoc />
    public IFeatureCollection Features => _features ??= new FeatureCollection();

    /// <summary>
    /// Gets a string that represents the current object.
    /// </summary>
    /// <returns>
    /// A string that represents the current object.
    /// </returns>
    public override string ToString()
        => RewriteEnumValue(this).ToString(true);

    /// <summary>
    /// Creates a new enum value.
    /// </summary>
    /// <param name="name">
    /// The name of the enum value.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="EnumValue"/>.
    /// </returns>
    public static EnumValue Create(string name) => new(name);
}
