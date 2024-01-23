using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Execution.Processing;

public static class OperationCompilerOptimizerHelper
{
    private const string _key = "HotChocolate.Execution.Utilities.SelectionSetOptimizer";

    public static void RegisterOptimizer(
        IDictionary<string, object?> contextData,
        IOperationCompilerOptimizer optimizer)
    {
        if (contextData.TryGetValue(_key, out var value) &&
            value is List<IOperationCompilerOptimizer> optimizers &&
            !optimizers.Contains(optimizer))
        {
            optimizers.Add(optimizer);
            return;
        }

        optimizers = [optimizer,];
        contextData[_key] = optimizers;
    }

    public static bool TryGetOptimizers(
        IReadOnlyDictionary<string, object?> contextData,
        [NotNullWhen(true)] out IReadOnlyList<IOperationCompilerOptimizer>? optimizers)
    {
        if (contextData.TryGetValue(_key, out var value) &&
            value is List<IOperationCompilerOptimizer> o)
        {
            optimizers = o;
            return true;
        }

        optimizers = null;
        return false;
    }
}
