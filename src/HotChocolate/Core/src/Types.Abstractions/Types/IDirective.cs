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
public interface IDirective : INameProvider, ISyntaxNodeProvider<DirectiveNode>
{
    /// <summary>
    /// Gets the <see cref="IDirectiveDefinition"/> that defines this directive.
    /// </summary>
    IDirectiveDefinition Definition { get; }

    /// <summary>
    /// Gets the arguments of the directive.
    /// </summary>
    ArgumentAssignmentCollection Arguments { get; }

    /// <summary>
    /// Converts the directive to a value of the specified type.
    /// </summary>
    /// <typeparam name="T">The type to convert the directive to.</typeparam>
    /// <returns>The value of the directive.</returns>
    T ToValue<T>() where T : notnull;
}
