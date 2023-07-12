using HotChocolate.Execution;
using HotChocolate.Language;
using ErrorHelper = HotChocolate.Fusion.Utilities.ErrorHelper;

namespace HotChocolate.Fusion.Execution;

internal static class FederatedQueryExecutor
{
    public static async Task<IExecutionResult> ExecuteAsync(
        FusionExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var operation = context.OperationContext.Operation;

        if (operation.Type is OperationType.Query or OperationType.Mutation &&
            !operation.HasIncrementalParts)
        {
            return await context.QueryPlan
                .ExecuteAsync(context, cancellationToken)
                .ConfigureAwait(false);
        }

        if (operation.Type is OperationType.Subscription)
        {
            return await context.QueryPlan
                .SubscribeAsync(context, cancellationToken)
                .ConfigureAwait(false);
        }

        return ErrorHelper.IncrementalDelivery_NotSupported();
    }
}
