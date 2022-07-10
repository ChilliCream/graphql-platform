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
        }
        else
        {
            for (var i = 0; i < context.Optimizers.Count; i++)
            {
                context.Optimizers[i].OptimizeSelectionSet(optimizerContext);
            }
        }

        if (_newSelections.Count > 0)
        {
            for (var i = 0; i < _newSelections.Count; i++)
            {
                if (_newSelections[i].SelectionSet is not null)
                {
                    var selectionSetInfo = new SelectionSetInfo(_newSelections[i].SelectionSet!, 0);
                    _selectionLookup.Add(_newSelections[i], new[] { selectionSetInfo });
                }
            }

            _newSelections.Clear();
        }
    }

    private IImmutableList<ISelectionSetOptimizer> ResolveOptimizers(
        IImmutableList<ISelectionSetOptimizer> optimizers,
        IObjectField field)
    {
        if (!OperationCompilerOptimizerHelper.TryGetOptimizers(field.ContextData,
                out var fieldOptimizers))
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
