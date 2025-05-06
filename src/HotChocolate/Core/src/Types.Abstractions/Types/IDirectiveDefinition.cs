using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// <para>
/// A GraphQL schema describes directives which are used to annotate various parts of a
/// GraphQL document as an indicator that they should be evaluated differently by a
/// validator, executor, or client tool such as a code generator.
/// </para>
/// <para>
/// https://spec.graphql.org/draft/#sec-Type-System.Directives
/// </para>
/// </summary>
public interface IDirectiveDefinition
    : INameProvider
    , IDescriptionProvider
    , ISyntaxNodeProvider<DirectiveDefinitionNode>
    , ISchemaCoordinateProvider
{
    /// <summary>
    /// Defines if this directive is repeatable. Repeatable directives are often useful when
    /// the same directive should be used with different arguments at a single location,
    /// especially in cases where additional information needs to be provided to a type or
    /// schema extension via a directive
    /// </summary>
    bool IsRepeatable { get; }

    /// <summary>
    /// Gets the directive arguments.
    /// </summary>
    IReadOnlyFieldDefinitionCollection<IInputValueDefinition> Arguments { get; }

    /// <summary>
    /// Gets the locations where this directive type can be used to annotate
    /// a type system member.
    /// </summary>
    DirectiveLocation Locations { get; }

    /// <summary>
    /// Creates a <see cref="DirectiveDefinitionNode"/> from the current <see cref="IDirectiveDefinition"/>.
    /// </summary>
    /// <returns>
    /// Returns a <see cref="DirectiveDefinitionNode"/>.
    /// </returns>
    new DirectiveDefinitionNode ToSyntaxNode();
}
