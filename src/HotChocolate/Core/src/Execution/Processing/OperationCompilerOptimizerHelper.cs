using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// This helper class allows to add optimizers to context data or retrieve optimizers from context data.
/// </summary>
internal static class OperationCompilerOptimizerHelper
{
    private const string _key = "HotChocolate.Execution.Utilities.SelectionSetOptimizer";

    public static void RegisterOptimizer(
        IDictionary<string, object?> contextData,
        ISelectionSetOptimizer optimizer)
    {
        if (contextData.TryGetValue(_key, out var value)
            && value is ImmutableArray<ISelectionSetOptimizer> optimizers)
        {
            if (!optimizers.Contains(optimizer))
            {
                optimizers = optimizers.Add(optimizer);
                contextData[_key] = optimizers;
            }
            return;
        }

        contextData[_key] = ImmutableArray.Create(optimizer);
    }

    public static bool TryGetOptimizers(
        IReadOnlyDictionary<string, object?> contextData,
        [NotNullWhen(true)] out ImmutableArray<ISelectionSetOptimizer>? optimizers)
    {
        if (contextData.TryGetValue(_key, out var value)
            && value is ImmutableArray<ISelectionSetOptimizer> o)
        {
            optimizers = o;
            return true;
        }

        optimizers = null;
        return false;
    }
}
