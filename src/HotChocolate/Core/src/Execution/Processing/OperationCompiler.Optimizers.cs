using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Types;
using static HotChocolate.Execution.Processing.OperationCompilerOptimizerHelper;

namespace HotChocolate.Execution.Processing;

public partial class OperationCompiler
{
    private void OptimizeSelectionSet(CompilerContext context)
    {
        var optimizers = context.Optimizers;
        if (optimizers.Count == 0)
        {
            return;
        }

        var optimizerContext = new SelectionSetOptimizerContext(
            this,
            context,
            _selectionLookup,
            _contextData,
            _createFieldPipeline,
            context.Path);

        var count = optimizers.Count;
        optimizers[0].OptimizeSelectionSet(optimizerContext);
        for (var i = 1; i < count; i++)
        {
            optimizers[i].OptimizeSelectionSet(optimizerContext);
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
