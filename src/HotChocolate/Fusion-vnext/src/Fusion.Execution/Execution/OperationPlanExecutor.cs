using System.Net.Mime;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

internal sealed class OperationPlanExecutor
{
    public async Task<IExecutionResult> ExecuteAsync(
        RequestContext requestContext,
        IVariableValueCollection variables,
        OperationPlan operationPlan,
        CancellationToken cancellationToken)
    {
        // We create a new CancellationTokenSource that can be used to halt the execution engine,
        // without also cancelling the entire request pipeline.
        using var executionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var context = new OperationPlanContext(requestContext, variables, operationPlan, executionCts);
        context.Begin();

        switch (operationPlan.Operation.Definition.Operation)
        {
            case OperationType.Query:
                await ExecuteQueryAsync(context, operationPlan, executionCts.Token);
                break;

            case OperationType.Mutation:
                await ExecuteMutationAsync(context, operationPlan, executionCts.Token);
                break;

            default:
                throw new InvalidOperationException("Only queries and mutations can be executed.");
        }

        // If the original CancellationToken of the request was cancelled,
        // the Execution nodes and the PlanExecutor should have been gracefully cancelled,
        // so we throw here to properly cancel the request execution.
        cancellationToken.ThrowIfCancellationRequested();

        return context.Complete();
    }

    public async Task<IExecutionResult> SubscribeAsync(
        RequestContext requestContext,
        OperationPlan operationPlan,
        CancellationToken cancellationToken)
    {
        // subscription plans must have a single root,
        // which represents the subscription to a source schema.
        var root = operationPlan.RootNodes.Single();

        // In the case of a subscription the initial node must always be an operation node
        // that represents the subscription to a specific source schema.
        if (root is not OperationExecutionNode subscriptionNode)
        {
            throw new InvalidOperationException("The specified operation plan is not supported.");
        }

        using var executionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var context = new OperationPlanContext(requestContext, operationPlan, executionCts);

        // TODO: We need to differentiate between request cancellations and haltings for individual messages
        return await ExecuteSubscriptionAsync(
            context,
            subscriptionNode,
            executionCts.Token);
    }

    private static async Task ExecuteQueryAsync(
        OperationPlanContext context,
        OperationPlan plan,
        CancellationToken cancellationToken)
    {
        var executionState = context.ExecutionState;

        cancellationToken.Register(() => executionState.Signal.TryResetToIdle());

        // GraphQL queries allow us to execute the plan by using full parallelism.
        // We fill the backlog with all nodes from the operation plan.
        executionState.FillBacklog(plan);

        // Then we start all root nodes as they can be processed in parallel.
        foreach (var root in plan.RootNodes)
        {
            executionState.StartNode(context, root, cancellationToken);
        }

        while (!cancellationToken.IsCancellationRequested
            && executionState.IsProcessing())
        {
            while (executionState.TryDequeueCompletedResult(out var result))
            {
                var node = plan.GetNodeById(result.Id);
                executionState.CompleteNode(node, result);
            }

            executionState.EnqueueNextNodes(context, cancellationToken);

            if (cancellationToken.IsCancellationRequested
                || !executionState.IsProcessing())
            {
                break;
            }

            // The signal will be set every time a node completes and will release the executor
            // from the async wait to go through the completed results.
            await executionState.Signal;
        }

        if (context.CollectTelemetry)
        {
            context.Traces = [.. executionState.Traces];
        }
    }

    private static async Task ExecuteMutationAsync(
        OperationPlanContext context,
        OperationPlan plan,
        CancellationToken cancellationToken)
    {
        var executionState = context.ExecutionState;

        cancellationToken.Register(() => executionState.Signal.TryResetToIdle());

        // For mutations, we fill the backlog with all nodes from the operation plan just like for queries.
        executionState.FillBacklog(plan);

        // The difference here is that the planner has one root node for each mutation field.
        // We execute the root nodes one after the other to cater for the GraphQL spec mutation algorithm
        // that requires sequential execution the roots but allows for parallel execution of their subtrees.
        foreach (var root in plan.RootNodes)
        {
            // We start the first root ...
            executionState.StartNode(context, root, cancellationToken);

            // ... and then process the subtree until its complete.
            // This is why in the mutation algorithm check for `HasActiveNodes` instead of `IsProcessing` which
            // would be true as long as there are items on the backlog. `HasActiveNodes` however will be true
            // as long as there are active nodes that result from processing the current subtree.
            while (!cancellationToken.IsCancellationRequested && executionState.HasActiveNodes())
            {
                while (executionState.TryDequeueCompletedResult(out var result))
                {
                    var node = plan.GetNodeById(result.Id);
                    executionState.CompleteNode(node, result);
                }

                executionState.EnqueueNextNodes(context, cancellationToken);

                if (cancellationToken.IsCancellationRequested
                    || !executionState.HasActiveNodes())
                {
                    break;
                }

                // The signal will be set every time a node completes and will release the executor
                // from the async wait to go through the completed results.
                await executionState.Signal;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }

        if (context.CollectTelemetry)
        {
            context.Traces = [.. executionState.Traces];
        }
    }

    private async Task<IExecutionResult> ExecuteSubscriptionAsync(
        OperationPlanContext context,
        OperationExecutionNode subscriptionNode,
        CancellationToken cancellationToken)
    {
        var executionState = context.ExecutionState;
        var plan = context.OperationPlan;

        var subscription = await subscriptionNode.SubscribeAsync(context, cancellationToken);

        cancellationToken.Register(() => executionState.Signal.TryResetToIdle());

        if (subscription.Status is not ExecutionStatus.Success)
        {
            throw new InvalidOperationException("We could not subscribe to the underlying source schema.");
        }

        var stream = new ResponseStream(CreateResponseStream);
        stream.RegisterForCleanup(context);
        return stream;

        async IAsyncEnumerable<IOperationResult> CreateResponseStream()
        {
            await foreach (var eventArgs in subscription.ReadStreamAsync().WithCancellation(cancellationToken))
            {
                IOperationResult result;

                try
                {
                    // CTS needs to be reset here as well

                    context.Begin(eventArgs.StartTimestamp, eventArgs.Activity?.TraceId.ToHexString());

                    executionState.Reset();
                    executionState.FillBacklog(plan);
                    executionState.EnqueueForCompletion(
                        new ExecutionNodeResult(
                            subscriptionNode.Id,
                            eventArgs.Activity,
                            eventArgs.Status,
                            eventArgs.Duration,
                            Exception: null,
                            DependentsToExecute: [],
                            VariableValueSets: eventArgs.VariableValueSets));

                    while (!cancellationToken.IsCancellationRequested && executionState.IsProcessing())
                    {
                        while (executionState.TryDequeueCompletedResult(out var nodeResult))
                        {
                            var node = plan.GetNodeById(nodeResult.Id);
                            executionState.CompleteNode(node, nodeResult);
                        }

                        executionState.EnqueueNextNodes(context, cancellationToken);

                        if (cancellationToken.IsCancellationRequested
                            || !executionState.IsProcessing())
                        {
                            break;
                        }

                        // The signal will be set every time a node completes and will release the executor
                        // from the async wait to go through the completed results.
                        await executionState.Signal;
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    result = context.Complete();
                }
                finally
                {
                    // disposing the eventArgs disposes the telemetry scope.
                    eventArgs.Dispose();
                }

                yield return result;
            }
        }
    }
}
