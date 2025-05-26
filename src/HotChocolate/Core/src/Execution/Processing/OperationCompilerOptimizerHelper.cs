using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// This helper class allows adding optimizers to context data or retrieve optimizers from context data.
/// </summary>
internal static class OperationCompilerOptimizerHelper
{
    public static void RegisterOptimizer(
        ObjectFieldConfiguration configuration,
        ISelectionSetOptimizer optimizer)
        => RegisterOptimizerInternal(configuration, optimizer);

    public static void RegisterOptimizer(
        ObjectField field,
        ISelectionSetOptimizer optimizer)
        => RegisterOptimizerInternal(field, optimizer);

    private static void RegisterOptimizerInternal(
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
        out ImmutableArray<ISelectionSetOptimizer> optimizers)
        => featureProvider.Features.TryGet(out optimizers);
}
