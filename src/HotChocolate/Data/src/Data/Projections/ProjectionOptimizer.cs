using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Projections;

internal sealed class ProjectionOptimizer(IProjectionProvider provider)
    : ISelectionSetOptimizer
{
    public void OptimizeSelectionSet(SelectionSetOptimizerContext context)
    {
        var processedSelections = new HashSet<string>();
        while (!processedSelections.SetEquals(context.Selections.Select(t => t.ResponseName)))
        {
            var selectionToProcess = new HashSet<string>(context.Selections.Select(t => t.ResponseName));
            selectionToProcess.ExceptWith(processedSelections);
            foreach (var responseName in selectionToProcess)
            {
                var selection = context.GetSelection(responseName);
                var rewrittenSelection =
                    provider.RewriteSelection(
                        context,
                        selection);

                context.ReplaceSelection(rewrittenSelection);

                processedSelections.Add(responseName);
            }
        }
    }
}
