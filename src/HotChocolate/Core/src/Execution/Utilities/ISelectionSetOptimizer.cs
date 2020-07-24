namespace HotChocolate.Execution.Utilities
{
    /// <summary>
    /// A selection-set optimizer can add additional internal selections,
    /// apply conditions to selections or remove selections.
    /// </summary>
    public interface ISelectionSetOptimizer
    {
        /// <summary>
        /// Optimize a selection-set for processing.
        /// </summary>
        /// <param name="context">
        /// The optimizer context.
        /// </param>
        void Optimize(SelectionSetOptimizerContext context);
    }
}
