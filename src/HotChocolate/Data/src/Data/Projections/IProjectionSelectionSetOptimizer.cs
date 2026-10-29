using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Projections;

internal interface IProjectionSelectionSetOptimizer
{
    void OptimizeSelectionSet(SelectionSetOptimizerContext context);
}
