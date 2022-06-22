using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

public partial class OperationCompiler
{
    private void OptimizeSelectionSet(CompilerContext context)
    {
        if (context.Optimizers.Count == 0)
        {
            return;
        }

        var optimizerContext = new SelectionSetOptimizerContext(
            this,
            context,
            _selectionLookup,
            _contextData);

        if (context.Optimizers.Count == 1)
        {
            context.Optimizers[0].OptimizeSelectionSet(optimizerContext);
            return;
        }

        for (var i = 0; i < context.Optimizers.Count; i++)
        {
            context.Optimizers[i].OptimizeSelectionSet(optimizerContext);
        }
    }

    private IImmutableList<ISelectionSetOptimizer> ResolveOptimizers(
        IImmutableList<ISelectionSetOptimizer> optimizers,
        IObjectField field)
    {
        if (!OperationCompilerOptimizerHelper.TryGetOptimizers(field.ContextData, out var fieldOptimizers))
        {
            return optimizers;
        }

        PrepareOptimizers(fieldOptimizers);

        foreach (var optimizer in _selectionSetOptimizers)
        {
            if (!optimizers.Contains(optimizer))
            {
                optimizers = optimizers.Add(optimizer);
            }
        }

        return optimizers;
    }
}
