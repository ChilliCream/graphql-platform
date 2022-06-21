using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

public partial class OperationCompiler
{
    private void OptimizeSelectionSet(CompilerContext context)
    {
        /*
        if (context.Optimizers.Count == 0)
        {
            return;
        }

        var optimizerContext = new SelectionOptimizerContext(this, context);

        if (context.Optimizers.Count == 1)
        {
            context.Optimizers[0].OptimizeSelectionSet(optimizerContext);
            return;
        }

        for (var i = 0; i < context.Optimizers.Count; i++)
        {
            context.Optimizers[i].OptimizeSelectionSet(optimizerContext);
        }

        */
    }

    private static IImmutableList<ISelectionOptimizer> ResolveOptimizers(
        IImmutableList<ISelectionOptimizer> optimizers,
        IObjectField field)
    {
        if (!SelectionOptimizerHelper.TryGetOptimizers(field.ContextData, out var fieldOptimizers))
        {
            return optimizers;
        }

        foreach (var optimizer in fieldOptimizers)
        {
            if (!optimizers.Contains(optimizer))
            {
                optimizers = optimizers.Add(optimizer);
            }
        }

        return optimizers;
    }
}
