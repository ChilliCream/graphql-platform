using System.Collections.Immutable;
using HotChocolate.Types;
using static HotChocolate.Execution.Processing.OperationCompilerOptimizerHelper;

namespace HotChocolate.Execution.Processing;

public partial class OperationCompiler
{
    private void OptimizeSelectionSet(CompilerContext context)
    {
        if (context.Optimizers.Length == 0)
        {
            return;
        }

        var optimizerContext =
            new SelectionSetOptimizerContext(
                this,
                context,
                _selectionLookup,
                _contextData,
                _createFieldPipeline,
                context.Path);

        if (context.Optimizers.Length == 1)
        {
            context.Optimizers[0].OptimizeSelectionSet(optimizerContext);
        }
        else
        {
            for (var i = 0; i < context.Optimizers.Length; i++)
            {
                context.Optimizers[i].OptimizeSelectionSet(optimizerContext);
            }
        }
    }

    private static ImmutableArray<ISelectionSetOptimizer> ResolveOptimizers(
        ImmutableArray<ISelectionSetOptimizer> optimizers,
        IObjectField field)
    {
        if (!TryGetOptimizers(field.ContextData, out var fieldOptimizers))
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
