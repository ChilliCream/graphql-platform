using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// This helper class allows to add optimizers to context data or retrieve optimizers from context data.
/// </summary>
internal static class OperationCompilerOptimizerHelper
{
    public static void RegisterOptimizer(
        IFeatureProvider featureProvider,
        ISelectionSetOptimizer optimizer)
    {
        var optimizers = featureProvider.Features.GetOrSet(ImmutableArray<ISelectionSetOptimizer>.Empty);

        if (!optimizers.Contains(optimizer))
        {
            optimizers = optimizers.Add(optimizer);
            featureProvider.Features.Set(optimizers);
        }
    }

    public static bool TryGetOptimizers(
        IFeatureProvider featureProvider,
        [NotNullWhen(true)] out ImmutableArray<ISelectionSetOptimizer>? optimizers)
        => featureProvider.Features.TryGet(out optimizers);
}
