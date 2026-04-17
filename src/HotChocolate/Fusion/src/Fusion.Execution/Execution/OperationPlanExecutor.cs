using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Text.Json;
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

        await using var context = requestContext.Schema.Services.GetRequiredService<OperationPlanContextPool>().Rent();
        context.Initialize(requestContext, variables, operationPlan, executionCts);

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

    public async Task<IExecutionResult> ExecuteWithDeferAsync(
        RequestContext requestContext,
        IVariableValueCollection variables,
        OperationPlan operationPlan,
        CancellationToken cancellationToken)
    {
        // Execute the main (non-deferred) plan nodes first.
        var executionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        OperationPlanContext? context = null;

        try
        {
            context = requestContext.Schema.Services.GetRequiredService<OperationPlanContextPool>().Rent();
            context.Initialize(requestContext, variables, operationPlan, executionCts);

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
                    throw new InvalidOperationException("Only queries and mutations can use @defer.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Build the initial result
            var initialResult = context.Complete();

            // Annotate the initial result with pending entries for each deferred group
            var deferredGroups = operationPlan.DeferredGroups;
            var pendingResults = ImmutableList.CreateBuilder<PendingResult>();

            foreach (var group in deferredGroups)
            {
                // Skip nested groups — they'll be announced when their parent completes
                if (group.Parent is not null)
                {
                    continue;
                }

                // If the defer is conditional, evaluate the condition
                if (group.IfVariable is not null)
                {
                    if (!variables.TryGetValue<BooleanValueNode>(group.IfVariable, out var boolValue))
                    {
                        throw new InvalidOperationException(
                            $"The variable {group.IfVariable} has an invalid value.");
                    }

                    if (!boolValue.Value)
                    {
                        continue;
                    }
                }

                pendingResults.Add(new PendingResult(
                    group.DeferId,
                    BuildPath(group.Path),
                    group.Label));
            }

            initialResult.HasNext = pendingResults.Count > 0;
            initialResult.Pending = pendingResults.ToImmutable();

            if (pendingResults.Count == 0)
            {
                // No active deferred groups (all conditions were false)
                executionCts.Dispose();
                await context.DisposeAsync();
                return initialResult;
            }

            // Return a ResponseStream that yields the initial result then deferred results
            var stream = new ResponseStream(
                () => CreateDeferredStream(
                    requestContext,
                    variables,
                    operationPlan,
                    initialResult,
                    cancellationToken),
                ExecutionResultKind.DeferredResult);

            stream.RegisterForCleanup(context);
            stream.RegisterForCleanup(executionCts);
            return stream;
        }
        catch (Exception)
        {
            executionCts.Dispose();

            if (context is { } c)
            {
                await c.DisposeAsync();
            }

            throw;
        }
    }

    private static async IAsyncEnumerable<OperationResult> CreateDeferredStream(
        RequestContext requestContext,
        IVariableValueCollection variables,
        OperationPlan operationPlan,
        OperationResult initialResult,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Yield the initial result first
        yield return initialResult;

        var deferredGroups = operationPlan.DeferredGroups;

        // Filter to only active groups (where @defer(if:) evaluates to true)
        var activeGroups = new List<DeferredExecutionGroup>();
        foreach (var group in deferredGroups)
        {
            if (group.IfVariable is not null)
            {
                if (!variables.TryGetValue<BooleanValueNode>(group.IfVariable, out var boolValue))
                {
                    throw new InvalidOperationException(
                        $"The variable {group.IfVariable} has an invalid value.");
                }

                if (!boolValue.Value)
                {
                    continue;
                }
            }

            // Only top-level groups start immediately; nested groups start when parent completes
            if (group.Parent is null)
            {
                activeGroups.Add(group);
            }
        }

        // Execute all top-level deferred groups in parallel using a channel
        var channel = Channel.CreateUnbounded<(DeferredExecutionGroup Group, OperationResult? Result, Exception? Error)>();
        var pendingCount = activeGroups.Count;

        foreach (var group in activeGroups)
        {
            _ = ExecuteDeferredGroupInBackground(
                requestContext, variables, operationPlan, group, channel.Writer, cancellationToken);
        }

        // Yield results as they complete
        while (pendingCount > 0 && !cancellationToken.IsCancellationRequested)
        {
            var (group, result, error) = await channel.Reader.ReadAsync(cancellationToken);
            pendingCount--;

            // Check if this group has children that should now start
            var childGroups = new List<DeferredExecutionGroup>();
            foreach (var candidate in deferredGroups)
            {
                if (candidate.Parent?.DeferId != group.DeferId)
                {
                    continue;
                }

                // If the child defer is conditional, evaluate the condition
                if (candidate.IfVariable is not null)
                {
                    if (!variables.TryGetValue<BooleanValueNode>(candidate.IfVariable, out var boolValue)
                        || !boolValue.Value)
                    {
                        continue;
                    }
                }

                childGroups.Add(candidate);
                pendingCount++;
                _ = ExecuteDeferredGroupInBackground(
                    requestContext,
                    variables,
                    operationPlan,
                    candidate,
                    channel.Writer,
                    cancellationToken);
            }

            // Build the incremental payload following the GraphQL incremental delivery spec:
            // - Deferred data goes in `incremental` array (not top-level `data`)
            // - `completed` signals the defer is done
            // - `hasNext` indicates if more payloads follow
            var isLast = pendingCount == 0;
            OperationResult payload;

            if (error is not null)
            {
                var errorObj = ErrorBuilder.New()
                    .SetMessage(error.Message)
                    .Build();
                payload = OperationResult.FromError(errorObj);
                payload.Completed = [new CompletedResult(group.DeferId, [errorObj])];
            }
            else if (result is not null)
            {
                payload = result;

                // Wrap the deferred result's data in IncrementalObjectResult
                // and clear top-level data/errors (per spec, subsequent payloads
                // use `incremental` array, not root `data`).
                //
                // The deferred plan executes against a standalone operation whose
                // result is rooted at Query (e.g. `{ user: { reviews: [...] } }`),
                // but the incremental delivery contract requires `incremental.data`
                // to be the delta at `pending.path`. We therefore navigate down
                // `group.Path` and emit only the subtree at that location.
                if (result.Data.HasValue
                    && !result.Data.Value.IsValueNull
                    && TryCreateIncrementalData(result.Data.Value, group.Path, out var incrementalData))
                {
                    payload.Incremental =
                    [
                        new IncrementalObjectResult(
                            group.DeferId,
                            result.Errors.Count > 0 ? result.Errors : null,
                            data: incrementalData)
                    ];
                    payload.Completed = [new CompletedResult(group.DeferId)];
                }
                else
                {
                    payload.Completed = [new CompletedResult(group.DeferId, result.Errors)];
                }

                // Per spec: subsequent payloads use `incremental` array, not root `data`.
                // We clear top-level data/errors so the formatter only renders
                // incremental delivery fields (incremental, completed, hasNext, pending).
                // The IncrementalDataFeature must be set first (via Incremental/Completed above)
                // so the Errors setter validation passes.
                payload.Data = null;
                if (payload.Errors.Count > 0)
                {
                    payload.Errors = [];
                }
            }
            else
            {
                // Empty deferred group — all fields may have been conditional and excluded.
                // Report a successful completion with no data.
                // We use FromError to create a valid OperationResult, then clear
                // top-level errors since this is a successful completion.
                var placeholder = ErrorBuilder.New()
                    .SetMessage("placeholder")
                    .Build();
                payload = OperationResult.FromError(placeholder);
                payload.Completed = [new CompletedResult(group.DeferId)];
                payload.Data = null;
                payload.Errors = [];
            }

            // Announce child pending results
            if (childGroups.Count > 0)
            {
                var childPending = ImmutableList.CreateBuilder<PendingResult>();
                foreach (var child in childGroups)
                {
                    childPending.Add(new PendingResult(
                        child.DeferId,
                        BuildPath(child.Path),
                        child.Label));
                }
                payload.Pending = childPending.ToImmutable();
            }

            payload.HasNext = !isLast;
            yield return payload;
        }
    }

    private static async Task ExecuteDeferredGroupInBackground(
        RequestContext requestContext,
        IVariableValueCollection variables,
        OperationPlan operationPlan,
        DeferredExecutionGroup group,
        ChannelWriter<(DeferredExecutionGroup, OperationResult?, Exception?)> writer,
        CancellationToken cancellationToken)
    {
        try
        {
            if (group.AllNodes.IsEmpty)
            {
                await writer.WriteAsync((group, null, null), cancellationToken);
                return;
            }

            // Create a mini OperationPlan for the deferred group using the group's
            // own compiled Operation for correct result mapping.
            var deferPlan = OperationPlan.Create(
                operationPlan.Id + "#defer_" + group.DeferId,
                group.Operation,
                group.RootNodes,
                group.AllNodes,
                [],
                0,
                0);

            using var executionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            await using var context = requestContext.Schema.Services
                .GetRequiredService<OperationPlanContextPool>().Rent();
            context.Initialize(requestContext, variables, deferPlan, executionCts);

            context.Begin();

            await ExecuteQueryAsync(context, deferPlan, executionCts.Token);

            var deferredResult = context.Complete();
            await writer.WriteAsync((group, deferredResult, null), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Write a cancellation result so the consumer doesn't hang
            await writer.WriteAsync((group, null, null), CancellationToken.None);
        }
        catch (Exception ex)
        {
            await writer.WriteAsync((group, null, ex), CancellationToken.None);
        }
    }

    private static Path BuildPath(SelectionPath selectionPath)
    {
        var path = Path.Root;

        for (var i = 0; i < selectionPath.Length; i++)
        {
            var segment = selectionPath[i];

            if (segment.Kind is SelectionPathSegmentKind.Field)
            {
                path = path.Append(segment.Name);
            }
        }

        return path;
    }

    /// <summary>
    /// Produces an <see cref="OperationResultData"/> whose logical root is the subtree
    /// at <paramref name="selectionPath"/> within the deferred plan's composite result.
    /// The incremental delivery contract requires <c>incremental.data</c> to be the
    /// delta to merge at the pending path, not the fully rooted result.
    /// </summary>
    private static bool TryCreateIncrementalData(
        OperationResultData rootData,
        SelectionPath selectionPath,
        out OperationResultData incrementalData)
    {
        if (rootData.Value is not CompositeResultDocument document)
        {
            // Unknown backing value: fall through to the default behaviour and
            // emit the result as-is.
            incrementalData = rootData;
            return true;
        }

        var element = document.Data;

        for (var i = 0; i < selectionPath.Length; i++)
        {
            var segment = selectionPath[i];

            // Inline fragments/type-conditions do not introduce an extra level
            // in the result tree, so we only walk field segments.
            if (segment.Kind is not SelectionPathSegmentKind.Field)
            {
                continue;
            }

            if (!element.TryGetProperty(segment.Name, out var next)
                || next.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                // The path could not be resolved or is null; nothing to merge.
                incrementalData = default;
                return false;
            }

            element = next;
        }

        // MemoryHolder is intentionally not carried over: the surrounding
        // OperationResult already owns the composite document's lifetime,
        // and the IncrementalObjectResult is a non-owning view over it.
        incrementalData = new OperationResultData(
            document,
            isValueNull: false,
            new DeferredPayloadDataFormatter(element),
            memoryHolder: null);
        return true;
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
            context = requestContext.Schema.Services.GetRequiredService<OperationPlanContextPool>().Rent();
            context.Initialize(requestContext, requestContext.VariableValues[0], operationPlan, executionCts);

            var subscriptionResult = await subscriptionNode.SubscribeAsync(context, executionCts.Token);
            var executionState = context.ExecutionState;

            cancellationRegistration = executionCts.Token.Register(
                static state => Unsafe.As<AsyncAutoResetEvent>(state)!.TryResetToIdle(),
                executionState.Signal);

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

            if (context is { } c)
            {
                await c.DisposeAsync();
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

        await using var cancellationRegistration = cancellationToken.Register(
            static state => Unsafe.As<AsyncAutoResetEvent>(state)!.TryResetToIdle(),
            executionState.Signal);

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

        await using var cancellationRegistration = cancellationToken.Register(
            static state => Unsafe.As<AsyncAutoResetEvent>(state)!.TryResetToIdle(),
            executionState.Signal);

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
            static state => Unsafe.As<AsyncAutoResetEvent>(state)!.TryResetToIdle(),
            executionState.Signal);

        var schemaName = subscriptionNode.SchemaName ?? context.GetDynamicSchemaName(subscriptionNode);

        await foreach (var eventArgs in stream)
        {
            using var scope = context.DiagnosticEvents.OnSubscriptionEvent(
                context,
                subscriptionNode,
                schemaName,
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
                    schemaName,
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
