namespace HotChocolate.Execution.Processing;

/// <summary>
/// A <see cref="SelectionSet"/> optimizer can add additional internal selections,
/// apply conditions to selections or optimize selections.
/// </summary>
public interface ISelectionSetOptimizer : IOperationCompilerOptimizer
{
    /// <summary>
    /// Is called to apply custom optimizations to a <see cref="SelectionSet"/>.
    /// </summary>
    /// <param name="context">
    /// The <see cref="SelectionSet"/> optimizer context.
    /// </param>
    void OptimizeSelectionSet(SelectionSetOptimizerContext context);
}
