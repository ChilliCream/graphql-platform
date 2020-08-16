using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Execution.Utilities
{
    public static class SelectionSetOptimizerHelper
    {
        private const string _key = "HotChocolate.Execution.Utilities.SelectionSetOptimizer";
        private static readonly ISelectionSetOptimizer[] _empty = new ISelectionSetOptimizer[0];

        public static void RegisterOptimizer(
            IDictionary<string, object?> contextData,
            ISelectionSetOptimizer optimizer)
        {
            if (contextData.TryGetValue(_key, out object? value) &&
                value is List<ISelectionSetOptimizer> optimizers &&
                !optimizers.Contains(optimizer))
            {
                optimizers.Add(optimizer);
                return;
            }

            optimizers = new List<ISelectionSetOptimizer> { optimizer };
            contextData[_key] = optimizers;
        }

        public static bool TryGetOptimizers(
            IReadOnlyDictionary<string, object?> contextData,
            [NotNullWhen(true)] out IReadOnlyList<ISelectionSetOptimizer>? optimizers)
        {
            if (contextData.TryGetValue(_key, out object? value) &&
                value is List<ISelectionSetOptimizer> o)
            {
                optimizers = o;
                return true;
            }

            optimizers = null;
            return false;
        }
    }
}
