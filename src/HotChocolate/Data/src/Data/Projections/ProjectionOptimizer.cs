using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Projections;

internal sealed class ProjectionOptimizer(IProjectionProvider provider)
    : ISelectionSetOptimizer
{
    public void OptimizeSelectionSet(SelectionSetOptimizerContext context)
    {
        var processedSelections = new HashSet<string>();
        while (!processedSelections.SetEquals(context.Selections.Keys))
        {
            var selectionToProcess = new HashSet<string>(context.Selections.Keys);
            selectionToProcess.ExceptWith(processedSelections);
            foreach (var responseName in selectionToProcess)
            {
                var rewrittenSelection =
                    provider.RewriteSelection(
                        context,
                        context.Selections[responseName]);

                context.ReplaceSelection(rewrittenSelection);

                processedSelections.Add(responseName);
            }
        }
    }
}
