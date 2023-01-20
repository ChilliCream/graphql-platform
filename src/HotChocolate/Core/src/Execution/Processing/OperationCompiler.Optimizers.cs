using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Types;
using static HotChocolate.Execution.Processing.OperationCompilerOptimizerHelper;

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
            _contextData,
            _createFieldPipeline);

        if (context.Optimizers.Count == 1)
        {
            context.Optimizers[0].OptimizeSelectionSet(optimizerContext);
        }
        else
        {
            for (var i = 0; i < context.Optimizers.Count; i++)
            {
                context.Optimizers[i].OptimizeSelectionSet(optimizerContext);
            }
        }
    }

    private IImmutableList<ISelectionSetOptimizer> ResolveOptimizers(
        IImmutableList<ISelectionSetOptimizer> optimizers,
        IObjectField field)
    {
        if (!TryGetOptimizers(field.ContextData, out var fieldOptimizers))
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
