using HotChocolate.Features;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

/// <summary>
/// Represents a GraphQL directive definition.
/// </summary>
public sealed class DirectiveDefinition(string name)
    : INamedTypeSystemMemberDefinition<DirectiveDefinition>
    , IDescriptionProvider
    , IFeatureProvider
{
    private string _name = name.EnsureGraphQLName();
    private InputFieldCollection? _arguments;
    private FeatureCollection? _features;

    /// <summary>
    /// Gets or sets the name of the directive.
    /// </summary>
    /// <value>
    /// The name of the directive.
    /// </value>
    public string Name
    {
        get => _name.EnsureGraphQLName();
        set => _name = value;
    }

    /// <summary>
    /// Gets or sets the description of the directive.
    /// </summary>
    /// <value>
    /// The description of the directive.
    /// </value>
    public string? Description { get; set; }

    /// <summary>
    /// Defines if this directive is repeatable and can be applied multiple times.
    /// </summary>
    public bool IsRepeatable { get; set; }

    /// <summary>
    /// Gets the arguments that are defined on this directive.
    /// </summary>
    public InputFieldCollection Arguments => _arguments ??= [];

    /// <summary>
    /// Gets the locations where this directive can be applied.
    /// </summary>
    /// <value>
    /// The locations where this directive can be applied.
    /// </value>
    public DirectiveLocation Locations { get; set; }

    /// <inheritdoc />
    public IFeatureCollection Features => _features ??= new FeatureCollection();

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
