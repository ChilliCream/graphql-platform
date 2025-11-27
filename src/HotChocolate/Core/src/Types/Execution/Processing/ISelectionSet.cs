using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// A selection set is primarily composed of field selections.
/// When needed a selection set can preserve fragments so that the execution engine
/// can branch the processing of these fragments.
/// </summary>
public interface ISelectionSet
{
    /// <summary>
    /// Gets an operation unique selection-set identifier of this selection.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Defines if this list needs post-processing for skip and include.
    /// </summary>
    bool IsConditional { get; }

    /// <summary>
    /// Gets the selections that shall be executed.
    /// </summary>
    IReadOnlyList<ISelection> Selections { get; }

    /// <summary>
    /// Gets the deferred fragments if any were preserved for execution.
    /// </summary>
    IReadOnlyList<IFragment> Fragments { get; }

    /// <summary>
    /// Gets the declaring operation.
    /// </summary>
    IOperation DeclaringOperation { get; }

    /// <summary>
    /// Tries to resolve a selection by name.
    /// </summary>
    /// <param name="responseName">
    /// The selection response name.
    /// </param>
    /// <param name="selection">
    /// The resolved selection.
    /// </param>
    /// <returns>
    /// Returns true if the selection was successfully resolved.
    /// </returns>
    bool TryGetSelection(string responseName, [NotNullWhen(true)] out ISelection? selection);

    /// <summary>
    /// Tries to resolve a selection by name.
    /// </summary>
    /// <param name="utf8ResponseName">
    /// The selection response name.
    /// </param>
    /// <param name="selection">
    /// The resolved selection.
    /// </param>
    /// <returns>
    /// Returns true if the selection was successfully resolved.
    /// </returns>
    bool TryGetSelection(ReadOnlySpan<byte> utf8ResponseName, [NotNullWhen(true)] out ISelection? selection);
}
