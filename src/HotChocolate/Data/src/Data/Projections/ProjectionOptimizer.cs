using System.Collections.Generic;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Data.Projections;

internal sealed class ProjectionOptimizer : ISelectionSetOptimizer
{
    private readonly IProjectionProvider _provider;

    public ProjectionOptimizer(IProjectionProvider provider)
    {
        _provider = provider;
    }

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
                    _provider.RewriteSelection(context, context.Selections[responseName]);
                context.ReplaceSelection(responseName, rewrittenSelection);
                processedSelections.Add(responseName);
            }
        }
    }
}
