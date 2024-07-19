using System.Collections.Immutable;
using HotChocolate.Execution.Processing;
using HotChocolate.Features;

namespace HotChocolate;

internal static class OperationCompilerSchemaExtensions
{
    public static ImmutableArray<IOperationOptimizer> GetOperationOptimizers(this ISchema schema)
        => schema.Features.Get<OperationCompilerFeature>()?.OperationOptimizers
            ?? ImmutableArray<IOperationOptimizer>.Empty;

    public static ImmutableArray<ISelectionSetOptimizer> GetSelectionSetOptimizers(this ISchema schema)
        => schema.Features.Get<OperationCompilerFeature>()?.SelectionSetOptimizers
            ?? ImmutableArray<ISelectionSetOptimizer>.Empty;
}
