using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Execution.Processing
{
    public static class SelectionOptimizerHelper
    {
        private const string _key = "HotChocolate.Execution.Utilities.SelectionSetOptimizer";
        private static readonly ISelectionOptimizer[] _empty = new ISelectionOptimizer[0];

        public static void RegisterOptimizer(
            IDictionary<string, object?> contextData,
            ISelectionOptimizer optimizer)
        {
            if (contextData.TryGetValue(_key, out object? value) &&
                value is List<ISelectionOptimizer> optimizers &&
                !optimizers.Contains(optimizer))
            {
                optimizers.Add(optimizer);
                return;
            }

            optimizers = new List<ISelectionOptimizer> { optimizer };
            contextData[_key] = optimizers;
        }

        public static bool TryGetOptimizers(
            IReadOnlyDictionary<string, object?> contextData,
            [NotNullWhen(true)] out IReadOnlyList<ISelectionOptimizer>? optimizers)
        {
            if (contextData.TryGetValue(_key, out object? value) &&
                value is List<ISelectionOptimizer> o)
            {
                optimizers = o;
                return true;
            }

            optimizers = null;
            return false;
        }
    }
}
