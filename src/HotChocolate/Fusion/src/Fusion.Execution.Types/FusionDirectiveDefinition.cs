using HotChocolate.Features;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Language;
using HotChocolate.Types;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;
using static HotChocolate.Serialization.SchemaDebugFormatter;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Types;

/// <summary>
/// Represents a GraphQL directive definition.
/// </summary>
public sealed class FusionDirectiveDefinition : IDirectiveDefinition
{
    private bool _completed;

    /// <summary>
    /// Represents a GraphQL directive definition.
    /// </summary>
    [Obsolete("Use the constructor overload that accepts isDeprecated and deprecationReason.")]
    public FusionDirectiveDefinition(
        string name,
        string? description,
        bool isRepeatable,
        FusionInputFieldDefinitionCollection arguments,
        DirectiveLocation locations)
        : this(
            name,
            description,
            isDeprecated: false,
            deprecationReason: null,
            isRepeatable,
            arguments,
            locations)
    {
    }

    /// <summary>
    /// Represents a GraphQL directive definition.
    /// </summary>
    public FusionDirectiveDefinition(
        string name,
        string? description,
        bool isDeprecated,
        string? deprecationReason,
        bool isRepeatable,
        FusionInputFieldDefinitionCollection arguments,
        DirectiveLocation locations)
    {
        name.EnsureGraphQLName();
        ArgumentNullException.ThrowIfNull(arguments);

        if (locations == 0)
        {
            throw new ArgumentException(
                "At least one directive location must be specified.",
                nameof(locations));
        }

        Name = name;
        Description = description;
        IsDeprecated = isDeprecated;
        DeprecationReason = deprecationReason;
        IsRepeatable = isRepeatable;
        Arguments = arguments;
        Locations = locations;
    }

    /// <summary>
    /// Gets the name of the directive.
    /// </summary>
    /// <value>
    /// The name of the directive.
    /// </value>
    public string Name { get; }

    /// <summary>
    /// Gets the description of the directive.
    /// </summary>
    /// <value>
    /// The description of the directive.
    /// </value>
    public string? Description { get; }

    /// <inheritdoc />
    public SchemaCoordinate Coordinate => new(Name, ofDirective: true);

    /// <summary>
    /// Defines if this directive is deprecated.
    /// </summary>
    public bool IsDeprecated { get; }

    /// <summary>
    /// Gets the reason why this directive is deprecated.
    /// </summary>
    public string? DeprecationReason { get; }

    /// <summary>
    /// Defines if this directive is repeatable and can be applied multiple times.
    /// </summary>
    public bool IsRepeatable { get; }

    /// <summary>
    /// Defines if this directive is publicly visible through introspection
    /// and external SDL output. Internal directives are part of the type system
    /// but hidden from external observers.
    /// </summary>
    public bool IsPublic { get; init; } = true;

    /// <summary>
    /// Gets the directives applied to this directive definition.
    /// </summary>
    public FusionDirectiveCollection Directives { get; private set; } =
        FusionDirectiveCollection.Empty;

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives => Directives;

    /// <summary>
    /// Gets the arguments that are defined on this directive.
    /// </summary>
    public FusionInputFieldDefinitionCollection Arguments { get; }

    IReadOnlyFieldDefinitionCollection<IInputValueDefinition> IDirectiveDefinition.Arguments
        => Arguments;

    /// <summary>
    /// Gets the locations where this directive can be applied.
    /// </summary>
    /// <value>
    /// The locations where this directive can be applied.
    /// </value>
    public DirectiveLocation Locations { get; }

    /// <summary>
    /// Gets the runtime type of the directive.
    /// </summary>
    public Type RuntimeType { get; } = typeof(object);

    internal void Complete(FusionDirectiveCollection directives)
    {
        ArgumentNullException.ThrowIfNull(directives);

        if (_completed)
        {
            throw new InvalidOperationException(
                "The directive definition has already been completed.");
        }

        Directives = directives;
        _completed = true;
    }

    /// <inheritdoc />
    public IFeatureCollection Features => field ??= new FeatureCollection();

    /// <summary>
    /// Gets a string that represents the current object.
    /// </summary>
    /// <returns>
    /// A string that represents the current object.
    /// </returns>
    public override string ToString() => Format(this).ToString(true);

    /// <summary>
    /// Creates a <see cref="DirectiveDefinitionNode"/>
    /// from a <see cref="FusionDirectiveDefinition"/>.
    /// </summary>
    public DirectiveDefinitionNode ToSyntaxNode() => Format(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode() => Format(this);
}
