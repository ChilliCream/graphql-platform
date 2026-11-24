namespace HotChocolate.Execution.Processing;

/// <summary>
/// Is notified when the selection set of a field was compiled for a type context.
/// </summary>
internal interface ISelectionSetHandler
{
    /// <summary>
    /// Is called after the selection set of the <paramref name="selection"/> was compiled.
    /// </summary>
    void OnSelectionSetCompiled(Selection selection, SelectionSet selectionSet);
}
