using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

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

        var pool = requestContext.Schema.Services.GetRequiredService<OperationPlanContextPool>();
        var context = pool.Rent();
        context.Initialize(requestContext, variables, operationPlan, executionCts);
        await using var _ = context;

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

        // We create a new CancellationTokenSource that can be used to halt the execution engine,
        // without also cancelling the entire request pipeline.
        var executionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        OperationPlanContext? context = null;
        CancellationTokenRegistration? cancellationRegistration = null;

        try
        {
            var pool = requestContext.Schema.Services.GetRequiredService<OperationPlanContextPool>();
            context = pool.Rent();
            context.Initialize(requestContext, requestContext.VariableValues[0], operationPlan, executionCts);

            var subscriptionResult = await subscriptionNode.SubscribeAsync(context, executionCts.Token);
            var executionState = context.ExecutionState;

            cancellationRegistration = executionCts.Token.Register(() => executionState.Signal.TryResetToIdle());

            if (subscriptionResult.Status is not ExecutionStatus.Success)
            {
                throw new InvalidOperationException("We could not subscribe to the underlying source schema.");
            }

            var subscriptionEnumerable = CreateSubscriptionEnumerable(
                context,
                subscriptionNode,
                subscriptionResult,
                executionCts.Token,
                cancellationToken);

            var stream = new ResponseStream(() => subscriptionEnumerable);
            stream.RegisterForCleanup(context);
            stream.RegisterForCleanup(executionCts);
            return stream;
        }
        catch (Exception)
        {
            executionCts.Dispose();

            if (cancellationRegistration is { } r)
            {
                await r.DisposeAsync();
            }

            throw;
        }
    }

    private static async Task ExecuteQueryAsync(
        OperationPlanContext context,
        OperationPlan plan,
        CancellationToken cancellationToken)
    {
        var executionState = context.ExecutionState;

        await using var cancellationRegistration =
            cancellationToken.Register(() => executionState.Signal.TryResetToIdle());

        // GraphQL queries allow us to execute the plan by using full parallelism.
        // We fill the backlog with all nodes from the operation plan.
        executionState.FillBacklog(plan);

        // Then we start all root nodes as they can be processed in parallel.
        foreach (var root in plan.RootNodes)
        {
            executionState.StartNode(context, root, cancellationToken);
        }

        while (!cancellationToken.IsCancellationRequested && executionState.IsProcessing())
        {
            while (executionState.TryDequeueCompletedResult(out var result))
            {
                var node = plan.GetNodeById(result.Id);
                executionState.CompleteNode(plan, node, result);
            }

            executionState.EnqueueNextNodes(context, cancellationToken);

            if (cancellationToken.IsCancellationRequested || !executionState.IsProcessing())
            {
                break;
            }

            // The signal will be set every time a node completes and will release the executor
            // from the async wait to go through the completed results.
            await executionState.Signal;
        }

        if (context.CollectTelemetry)
        {
            context.Traces = executionState.Traces.ToImmutableDictionary();
        }
    }

    private static async Task ExecuteMutationAsync(
        OperationPlanContext context,
        OperationPlan plan,
        CancellationToken cancellationToken)
    {
        var executionState = context.ExecutionState;

        await using var cancellationRegistration =
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
                    executionState.CompleteNode(plan, node, result);
                }

                executionState.EnqueueNextNodes(context, cancellationToken);

                if (cancellationToken.IsCancellationRequested || !executionState.HasActiveNodes())
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
            context.Traces = executionState.Traces.ToImmutableDictionary();
        }
    }

    private static async IAsyncEnumerable<OperationResult> CreateSubscriptionEnumerable(
        OperationPlanContext context,
        OperationExecutionNode subscriptionNode,
        SubscriptionResult subscriptionResult,
        [EnumeratorCancellation] CancellationToken executionCancellationToken,
        CancellationToken requestCancellationToken)
    {
        var plan = context.OperationPlan;
        var executionState = context.ExecutionState;
        var stream = subscriptionResult.ReadStreamAsync()
            .WithCancellation(executionCancellationToken);
        await using var cancellationRegistration = executionCancellationToken.Register(
            () => executionState.Signal.TryResetToIdle());

        await foreach (var eventArgs in stream)
        {
            using var scope = context.DiagnosticEvents.OnSubscriptionEvent(
                context,
                subscriptionNode,
                subscriptionNode.SchemaName ?? context.GetDynamicSchemaName(subscriptionNode),
                subscriptionResult.Id);

            OperationResult result;

            try
            {
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
                        SkippedDefinitions: [],
                        VariableValueSets: eventArgs.VariableValueSets));

                while (!executionCancellationToken.IsCancellationRequested && executionState.IsProcessing())
                {
                    while (executionState.TryDequeueCompletedResult(out var nodeResult))
                    {
                        var node = plan.GetNodeById(nodeResult.Id);
                        executionState.CompleteNode(plan, node, nodeResult);
                    }

                    executionState.EnqueueNextNodes(context, executionCancellationToken);

                    if (executionCancellationToken.IsCancellationRequested || !executionState.IsProcessing())
                    {
                        break;
                    }

                    // The signal will be set every time a node completes and will release the executor
                    // from the async wait to go through the completed results.
                    await executionState.Signal;
                }

                // If the original CancellationToken of the request was cancelled,
                // the Execution nodes and the PlanExecutor should have been gracefully cancelled,
                // so we throw here to properly cancel the request execution.
                requestCancellationToken.ThrowIfCancellationRequested();

                result = context.Complete(reusable: true);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.DiagnosticEvents.SubscriptionEventError(
                    context,
                    subscriptionNode,
                    subscriptionNode.SchemaName ?? context.GetDynamicSchemaName(subscriptionNode),
                    subscriptionResult.Id,
                    ex);
                throw;
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
