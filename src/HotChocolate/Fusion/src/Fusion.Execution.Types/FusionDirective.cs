using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Serialization;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

/// <summary>
/// Represents a GraphQL directive in a fusion schema.
/// </summary>
public sealed class FusionDirective : IDirective
{
    /// <summary>
    /// Initializes a new instance of <see cref="FusionDirective"/>.
    /// </summary>
    /// <param name="definition">The directive definition.</param>
    /// <param name="arguments">The arguments applied to the directive.</param>
    public FusionDirective(
        FusionDirectiveDefinition definition,
        params ImmutableArray<ArgumentAssignment> arguments)
    {
        ArgumentNullException.ThrowIfNull(definition);

        Definition = definition;
        Arguments = new ArgumentAssignmentCollection(arguments);
    }

    /// <summary>
    /// Gets the name of this directive.
    /// </summary>
    public string Name => Definition.Name;

    /// <summary>
    /// Gets the directive definition.
    /// </summary>
    public FusionDirectiveDefinition Definition { get; }

    IDirectiveDefinition IDirective.Definition => Definition;

    /// <summary>
    /// Gets the collection of arguments applied to this directive.
    /// </summary>
    public ArgumentAssignmentCollection Arguments { get; }

    /// <summary>
    /// Gets a string that represents the current object.
    /// </summary>
    /// <returns>
    /// A string that represents the current object.
    /// </returns>
    public override string ToString()
        => SchemaDebugFormatter.Format(this).ToString(true);

    /// <summary>
    /// Creates an <see cref="DirectiveNode"/> from an <see cref="FusionDirective"/>.
    /// </summary>
    public DirectiveNode ToSyntaxNode()
        => SchemaDebugFormatter.Format(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode()
        => SchemaDebugFormatter.Format(this);

    /// <summary>
    /// Converts the directive's arguments to a strongly typed value.
    /// </summary>
    /// <typeparam name="T">The type to convert the directive arguments to.</typeparam>
    /// <returns>A strongly typed value representing the directive's arguments.</returns>
    /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
    public T ToValue<T>() where T : notnull
        => throw new NotImplementedException();
}
