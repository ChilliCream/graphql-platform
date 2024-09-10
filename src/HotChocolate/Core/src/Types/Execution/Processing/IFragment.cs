#nullable enable

using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents a deferred fragment.
/// </summary>
public interface IFragment : IOptionalSelection
{
    /// <summary>
    /// Gets the internal fragment identifier.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the type condition.
    /// </summary>
    IObjectType TypeCondition { get; }

    /// <summary>
    /// Gets the syntax node from the original GraphQL request document.
    /// </summary>
    ISyntaxNode SyntaxNode { get; }

    /// <summary>
    /// Gets the collection of directives that are annotated to this fragment.
    /// </summary>
    IReadOnlyList<DirectiveNode> Directives { get; }

    /// <summary>
    /// Gets the selection set of this fragment.
    /// </summary>
    ISelectionSet SelectionSet { get; }

    /// <summary>
    /// Gets the fragment label.
    /// </summary>
    /// <param name="variables">
    /// The variable values.
    /// </param>
    /// <returns>
    /// Returns the fragment label.
    /// </returns>
    string? GetLabel(IVariableValueCollection variables);
}
