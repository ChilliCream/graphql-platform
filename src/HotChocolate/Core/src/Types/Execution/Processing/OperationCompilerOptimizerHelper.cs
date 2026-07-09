using System.Collections.Immutable;
using HotChocolate.Features;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// This helper class allows to register optimizers with a field configuration.
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

    public static ImmutableArray<ISelectionSetOptimizer> GetOptimizers(Selection selection)
    {
        var optimizers = ImmutableArray<ISelectionSetOptimizer>.Empty;

        if (selection.Features.TryGet<ImmutableArray<ISelectionSetOptimizer>>(out var selectionOptimizers))
        {
            optimizers = selectionOptimizers;
        }

        if (selection.Field.Features.TryGet<ImmutableArray<ISelectionSetOptimizer>>(out var fieldOptimizers))
        {
            optimizers = optimizers.AddRange(fieldOptimizers);
        }

        return optimizers;
    }
}
