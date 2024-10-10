using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

/// <summary>
/// Represents a GraphQL directive definition.
/// </summary>
public sealed class CompositeDirectiveDefinition(
    string name,
    string? description,
    bool isRepeatable,
    CompositeInputFieldCollection arguments,
    DirectiveLocation locations)
{
    /// <summary>
    /// Gets the name of the directive.
    /// </summary>
    /// <value>
    /// The name of the directive.
    /// </value>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the description of the directive.
    /// </summary>
    /// <value>
    /// The description of the directive.
    /// </value>
    public string? Description { get; } = description;

    /// <summary>
    /// Defines if this directive is repeatable and can be applied multiple times.
    /// </summary>
    public bool IsRepeatable { get; } = isRepeatable;

    /// <summary>
    /// Gets the arguments that are defined on this directive.
    /// </summary>
    public CompositeInputFieldCollection Arguments { get; } = arguments;

    /// <summary>
    /// Gets the locations where this directive can be applied.
    /// </summary>
    /// <value>
    /// The locations where this directive can be applied.
    /// </value>
    public DirectiveLocation Locations { get; } = locations;
}
