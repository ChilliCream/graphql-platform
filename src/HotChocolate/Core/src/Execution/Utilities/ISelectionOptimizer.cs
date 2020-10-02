using HotChocolate.Language;

namespace HotChocolate.Execution.Utilities
{
    /// <summary>
    /// A selection optimizer can add additional internal selections,
    /// apply conditions to selections, remove selections or optimize fragments.
    /// </summary>
    public interface ISelectionOptimizer
    {
        /// <summary>
        /// Optimize a selection-set for the execution engine.
        /// </summary>
        /// <param name="context">
        /// The optimizer context.
        /// </param>
        void OptimizeSelectionSet(
            SelectionOptimizerContext context);

        /// <summary>
        /// Defines if a fragment can be deferred.
        /// </summary>
        /// <param name="context">
        /// The optimizer context.
        /// </param>
        /// <param name="fragment">
        /// The fragment that is deferrable.
        /// </param>
        bool AllowFragmentDeferral(
            SelectionOptimizerContext context,
            InlineFragmentNode fragment);

        /// <summary>
        /// Defines if a fragment can be deferred.
        /// </summary>
        /// <param name="context">
        /// The optimizer context.
        /// </param>
        /// <param name="fragmentSpread">
        /// The fragment spread.
        /// </param>
        /// <param name="fragmentDefinition">
        /// The fragment definition.
        /// </param>
        bool AllowFragmentDeferral(
            SelectionOptimizerContext context,
            FragmentSpreadNode fragmentSpread,
            FragmentDefinitionNode fragmentDefinition);
    }
}
