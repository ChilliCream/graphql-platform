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

internal static class OperationPlanExecutor
{
    public static async Task<IExecutionResult> ExecuteAsync(
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

    public static async Task<IExecutionResult> ExecuteWithDeferAsync(
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

            // Build the initial result.
            var initialResult = context.Complete();

            // Compute the active delivery groups (one per @defer occurrence whose
            // @defer(if:) evaluates to true) and the subplans that will actually run.
            // A subplan is active if at least one of its delivery groups is active.
            var activeDeliveryGroupIds = new HashSet<int>();
            foreach (var deliveryGroup in operationPlan.DeliveryGroups)
            {
                if (IsDeliveryGroupActive(deliveryGroup, variables))
                {
                    activeDeliveryGroupIds.Add(deliveryGroup.Id);
                }
            }

            // Announce every top-level active delivery group as pending on the
            // initial payload. Nested delivery groups are announced when their
            // parent's subplan completes.
            var pendingResults = ImmutableList.CreateBuilder<PendingResult>();
            foreach (var deliveryGroup in operationPlan.DeliveryGroups)
            {
                if (deliveryGroup.Parent is not null)
                {
                    continue;
                }

                if (!activeDeliveryGroupIds.Contains(deliveryGroup.Id))
                {
                    continue;
                }

                pendingResults.Add(new PendingResult(
                    deliveryGroup.Id,
                    BuildPath(deliveryGroup.Path ?? SelectionPath.Root),
                    deliveryGroup.Label));
            }

            initialResult.HasNext = pendingResults.Count > 0;
            initialResult.Pending = pendingResults.ToImmutable();

            if (pendingResults.Count == 0)
            {
                // No active deferred subplans (all conditions were false).
                executionCts.Dispose();
                await context.DisposeAsync();
                return initialResult;
            }

            // Return a ResponseStream that yields the initial result then deferred results.
            var stream = new ResponseStream(
                () => CreateDeferredStream(
                    requestContext,
                    variables,
                    operationPlan,
                    initialResult,
                    activeDeliveryGroupIds,
                    cancellationToken),
                ExecutionResultKind.DeferredResult);

            stream.RegisterForCleanup(context);
            stream.RegisterForCleanup(executionCts);
            return stream;
        }
        catch (Exception)
        {
            executionCts.Dispose();

            if (context is not null)
            {
                await context.DisposeAsync();
            }

            throw;
        }
    }

    private static async IAsyncEnumerable<OperationResult> CreateDeferredStream(
        RequestContext requestContext,
        IVariableValueCollection variables,
        OperationPlan operationPlan,
        OperationResult initialResult,
        HashSet<int> activeDeliveryGroupIds,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Yield the initial result first.
        yield return initialResult;

        var deferredSubPlans = operationPlan.DeferredSubPlans;

        // Per-delivery-group completion tracking. A delivery group is considered
        // complete when every subplan whose DeliveryGroups contains it has
        // finished. We also track which subplans are "active" so an inactive
        // @defer(if: false) group does not block completion accounting.
        var pendingCountByDeliveryGroup = new Dictionary<int, int>();
        foreach (var subPlan in deferredSubPlans)
        {
            if (!IsSubPlanActive(subPlan, activeDeliveryGroupIds))
            {
                continue;
            }

            foreach (var deliveryGroup in subPlan.DeliveryGroups)
            {
                if (!activeDeliveryGroupIds.Contains(deliveryGroup.Id))
                {
                    continue;
                }

                pendingCountByDeliveryGroup[deliveryGroup.Id] =
                    pendingCountByDeliveryGroup.GetValueOrDefault(deliveryGroup.Id) + 1;
            }
        }

        // A subplan starts running once every delivery group it depends on has
        // had its parent subplan dispatched. For now we keep the simpler rule
        // used previously: a subplan is top-level when its first delivery group
        // has no parent. Nested subplans are launched when their parent delivery
        // group's subplan completes.
        var started = new HashSet<ExecutionSubPlan>();
        var channel = Channel.CreateUnbounded<(ExecutionSubPlan SubPlan, OperationResult? Result, Exception? Error)>();
        var pendingSubPlanCount = 0;

        foreach (var subPlan in deferredSubPlans)
        {
            if (!IsSubPlanActive(subPlan, activeDeliveryGroupIds))
            {
                continue;
            }

            if (subPlan.DeliveryGroups[0].Parent is not null)
            {
                continue;
            }

            started.Add(subPlan);
            pendingSubPlanCount++;
            _ = ExecuteDeferredSubPlanInBackground(
                requestContext, variables, operationPlan, subPlan, channel.Writer, cancellationToken);
        }

        // Track which delivery groups we have already announced as pending so
        // we do not re-announce nested groups multiple times when they belong
        // to more than one subplan.
        var announcedDeliveryGroupIds = new HashSet<int>();
        foreach (var deliveryGroup in operationPlan.DeliveryGroups)
        {
            if (deliveryGroup.Parent is null && activeDeliveryGroupIds.Contains(deliveryGroup.Id))
            {
                announcedDeliveryGroupIds.Add(deliveryGroup.Id);
            }
        }

        // Yield results as they complete.
        while (pendingSubPlanCount > 0 && !cancellationToken.IsCancellationRequested)
        {
            var (subPlan, result, error) = await channel.Reader.ReadAsync(cancellationToken);
            pendingSubPlanCount--;

            // Start nested subplans whose parent delivery group belongs to the
            // just-completed subplan, then announce their delivery groups as
            // pending. We collect announcements for the outgoing payload.
            var childPending = ImmutableList.CreateBuilder<PendingResult>();
            foreach (var candidate in deferredSubPlans)
            {
                if (!IsSubPlanActive(candidate, activeDeliveryGroupIds))
                {
                    continue;
                }

                if (started.Contains(candidate))
                {
                    continue;
                }

                var candidateParent = candidate.DeliveryGroups[0].Parent;
                if (candidateParent is null)
                {
                    continue;
                }

                var parentBelongsToJustCompleted = false;
                foreach (var deliveryGroup in subPlan.DeliveryGroups)
                {
                    if (ReferenceEquals(deliveryGroup, candidateParent))
                    {
                        parentBelongsToJustCompleted = true;
                        break;
                    }
                }

                if (!parentBelongsToJustCompleted)
                {
                    continue;
                }

                started.Add(candidate);
                pendingSubPlanCount++;
                _ = ExecuteDeferredSubPlanInBackground(
                    requestContext,
                    variables,
                    operationPlan,
                    candidate,
                    channel.Writer,
                    cancellationToken);

                foreach (var deliveryGroup in candidate.DeliveryGroups)
                {
                    if (!activeDeliveryGroupIds.Contains(deliveryGroup.Id))
                    {
                        continue;
                    }

                    if (!announcedDeliveryGroupIds.Add(deliveryGroup.Id))
                    {
                        continue;
                    }

                    childPending.Add(new PendingResult(
                        deliveryGroup.Id,
                        BuildPath(deliveryGroup.Path ?? SelectionPath.Root),
                        deliveryGroup.Label));
                }
            }

            // Pick the best delivery group for this subplan's emission: the
            // one whose Path is the longest prefix of the data's actual path
            // (equivalently: produces the shortest subPath). This follows the
            // graphql-js `_getBestIdAndSubPath` rule. Ties are broken by the
            // smallest DeferUsage.Id for determinism, which matches the sorted
            // DeliveryGroups order.
            var bestDeliveryGroup = PickBestDeliveryGroup(subPlan);

            // Build the incremental payload following the GraphQL incremental
            // delivery spec. Deferred data goes in `incremental`; `completed`
            // signals a delivery group is done; `hasNext` indicates more
            // payloads follow. We compute completed entries by decrementing
            // each delivery group the subplan contributed to.
            var completed = ImmutableList.CreateBuilder<CompletedResult>();
            OperationResult payload;

            if (error is not null)
            {
                var errorObj = ErrorBuilder.New()
                    .SetMessage(error.Message)
                    .Build();
                payload = OperationResult.FromError(errorObj);
                CompleteDeliveryGroupsForSubPlan(
                    subPlan,
                    activeDeliveryGroupIds,
                    pendingCountByDeliveryGroup,
                    completed,
                    errors: [errorObj]);
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
                // to be the delta at `pending.path`. We navigate down the best
                // delivery group's path and emit only the subtree at that location.
                if (result.Data.HasValue
                    && !result.Data.Value.IsValueNull
                    && TryCreateIncrementalData(
                        result.Data.Value,
                        bestDeliveryGroup,
                        out var incrementalData))
                {
                    payload.Incremental =
                    [
                        new IncrementalObjectResult(
                            bestDeliveryGroup.Id,
                            result.Errors.Count > 0 ? result.Errors : null,
                            data: incrementalData)
                    ];
                }

                CompleteDeliveryGroupsForSubPlan(
                    subPlan,
                    activeDeliveryGroupIds,
                    pendingCountByDeliveryGroup,
                    completed,
                    errors: result.Errors.Count > 0 && payload.Incremental.Count == 0
                        ? result.Errors
                        : null);
            }
            else
            {
                // Empty deferred subplan: all fields may have been conditional
                // and excluded. Report a successful completion with no data.
                // We use FromError to create a valid OperationResult, then
                // clear top-level errors since this is a successful completion.
                var placeholder = ErrorBuilder.New()
                    .SetMessage("placeholder")
                    .Build();
                payload = OperationResult.FromError(placeholder);
                CompleteDeliveryGroupsForSubPlan(
                    subPlan,
                    activeDeliveryGroupIds,
                    pendingCountByDeliveryGroup,
                    completed,
                    errors: null);
            }

            // Set Completed first so the IncrementalDataFeature is established
            // before clearing the top-level Errors (which validates against it).
            if (completed.Count > 0)
            {
                payload.Completed = completed.ToImmutable();
            }

            if (childPending.Count > 0)
            {
                payload.Pending = childPending.ToImmutable();
            }

            // Per spec: subsequent payloads use `incremental` array, not root
            // `data`. Clear top-level data/errors so the formatter only renders
            // incremental delivery fields.
            payload.Data = null;
            if (payload.Errors.Count > 0)
            {
                payload.Errors = [];
            }

            payload.HasNext = pendingSubPlanCount > 0;
            yield return payload;
        }
    }

    private static async Task ExecuteDeferredSubPlanInBackground(
        RequestContext requestContext,
        IVariableValueCollection variables,
        OperationPlan operationPlan,
        ExecutionSubPlan subPlan,
        ChannelWriter<(ExecutionSubPlan, OperationResult?, Exception?)> writer,
        CancellationToken cancellationToken)
    {
        try
        {
            if (subPlan.AllNodes.IsEmpty)
            {
                await writer.WriteAsync((subPlan, null, null), cancellationToken);
                return;
            }

            var representative = subPlan.DeliveryGroups[0];

            // Create a mini OperationPlan for the deferred subplan using the
            // subplan's own compiled Operation for correct result mapping.
            var deferPlan = OperationPlan.Create(
                operationPlan.Id + "#defer_" + representative.Id,
                subPlan.Operation,
                subPlan.RootNodes,
                subPlan.AllNodes,
                [],
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
            await writer.WriteAsync((subPlan, deferredResult, null), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Write a cancellation result so the consumer doesn't hang.
            await writer.WriteAsync((subPlan, null, null), CancellationToken.None);
        }
        catch (Exception ex)
        {
            await writer.WriteAsync((subPlan, null, ex), CancellationToken.None);
        }
    }

    private static bool IsDeliveryGroupActive(DeferUsage deliveryGroup, IVariableValueCollection variables)
    {
        if (deliveryGroup.IfVariable is null)
        {
            return true;
        }

        if (!variables.TryGetValue<BooleanValueNode>(deliveryGroup.IfVariable, out var boolValue))
        {
            throw new InvalidOperationException(
                $"The variable {deliveryGroup.IfVariable} has an invalid value.");
        }

        return boolValue.Value;
    }

    private static bool IsSubPlanActive(ExecutionSubPlan subPlan, HashSet<int> activeDeliveryGroupIds)
    {
        foreach (var deliveryGroup in subPlan.DeliveryGroups)
        {
            if (activeDeliveryGroupIds.Contains(deliveryGroup.Id))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Picks the best delivery group for emitting a subplan's incremental
    /// payload. Per graphql-js <c>_getBestIdAndSubPath</c>, the best group is
    /// the one whose <see cref="DeferUsage.Path"/> is the longest prefix of
    /// the data's actual path (equivalently, the shortest <c>subPath</c>).
    /// Ties are broken by the smallest <see cref="DeferUsage.Id"/>, which is
    /// the first element in the sorted <see cref="ExecutionSubPlan.DeliveryGroups"/>.
    /// </summary>
    private static DeferUsage PickBestDeliveryGroup(ExecutionSubPlan subPlan)
    {
        var best = subPlan.DeliveryGroups[0];
        var bestLength = best.Path?.Length ?? 0;

        for (var i = 1; i < subPlan.DeliveryGroups.Length; i++)
        {
            var candidate = subPlan.DeliveryGroups[i];
            var candidateLength = candidate.Path?.Length ?? 0;

            if (candidateLength > bestLength)
            {
                best = candidate;
                bestLength = candidateLength;
            }
        }

        return best;
    }

    private static void CompleteDeliveryGroupsForSubPlan(
        ExecutionSubPlan subPlan,
        HashSet<int> activeDeliveryGroupIds,
        Dictionary<int, int> pendingCountByDeliveryGroup,
        ImmutableList<CompletedResult>.Builder completed,
        IReadOnlyList<IError>? errors)
    {
        foreach (var deliveryGroup in subPlan.DeliveryGroups)
        {
            if (!activeDeliveryGroupIds.Contains(deliveryGroup.Id))
            {
                continue;
            }

            if (!pendingCountByDeliveryGroup.TryGetValue(deliveryGroup.Id, out var count))
            {
                continue;
            }

            count--;
            if (count <= 0)
            {
                pendingCountByDeliveryGroup.Remove(deliveryGroup.Id);
                completed.Add(errors is { Count: > 0 }
                    ? new CompletedResult(deliveryGroup.Id, errors)
                    : new CompletedResult(deliveryGroup.Id));
            }
            else
            {
                pendingCountByDeliveryGroup[deliveryGroup.Id] = count;
            }
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
    /// Produces an <see cref="OperationResultData"/> whose logical root is the
    /// subtree at the best delivery group's path within the deferred plan's
    /// composite result. The incremental delivery contract requires
    /// <c>incremental.data</c> to be the delta to merge at the pending path,
    /// not the fully rooted result.
    /// </summary>
    private static bool TryCreateIncrementalData(
        OperationResultData rootData,
        DeferUsage bestDeliveryGroup,
        out OperationResultData incrementalData)
    {
        if (rootData.Value is not CompositeResultDocument document)
        {
            // Unknown backing value: fall through to the default behavior and
            // emit the result as-is.
            incrementalData = rootData;
            return true;
        }

        var element = document.Data;
        var selectionPath = bestDeliveryGroup.Path ?? SelectionPath.Root;

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

    public static async Task<IExecutionResult> SubscribeAsync(
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

            var subscriptionEnumerable = CreateResponseStream(
                context,
                subscriptionNode,
                subscriptionResult,
                requestContext.Schema.Services.GetService<ExecutionConcurrencyGate>(),
                requestContext.Schema.GetRequestOptions().ExecutionTimeout,
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

    private static async IAsyncEnumerable<OperationResult> CreateResponseStream(
        OperationPlanContext context,
        OperationExecutionNode subscriptionNode,
        SubscriptionResult subscriptionResult,
        ExecutionConcurrencyGate? concurrencyGate,
        TimeSpan eventTimeout,
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

        // We allocate a single CancellationTokenSource per subscription and reuse it
        // across all events via TryReset(). The execution token is linked in so that
        // client-abort / server-shutdown still propagates.
        var eventCts = new CancellationTokenSource();
        var eventCtsRegistration = executionCancellationToken.UnsafeRegister(
            static state => Unsafe.As<CancellationTokenSource>(state)!.Cancel(),
            eventCts);

        var schemaName = subscriptionNode.SchemaName ?? context.GetDynamicSchemaName(subscriptionNode);

        try
        {
            await foreach (var eventArgs in stream)
            {
                using var scope = context.DiagnosticEvents.OnSubscriptionEvent(
                    context,
                    subscriptionNode,
                    schemaName,
                    subscriptionResult.Id);

                OperationResult result;

                var gateAcquired = false;

                // Arm the shared CTS for this event and derive the per-event token so
                // that each event is bounded by the configured execution timeout.
                eventCts.CancelAfter(eventTimeout);
                var eventToken = eventCts.Token;

                try
                {
                    if (concurrencyGate is { IsEnabled: true })
                    {
                        await concurrencyGate.WaitAsync(eventToken).ConfigureAwait(false);
                        gateAcquired = true;
                    }

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

                    while (!eventToken.IsCancellationRequested && executionState.IsProcessing())
                    {
                        while (executionState.TryDequeueCompletedResult(out var nodeResult))
                        {
                            var node = plan.GetNodeById(nodeResult.Id);
                            executionState.CompleteNode(plan, node, nodeResult);
                        }

                        executionState.EnqueueNextNodes(context, eventToken);

                        if (eventToken.IsCancellationRequested || !executionState.IsProcessing())
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
                    // If the event budget was exhausted, surface it as a cancellation so the
                    // stream tears down and the caller can observe the timeout.
                    eventToken.ThrowIfCancellationRequested();

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

                    if (gateAcquired)
                    {
                        concurrencyGate!.Release();
                    }

                    // Reset the shared CTS so the next event can start with a fresh budget.
                    // If TryReset() returns false the source was cancelled (timeout or
                    // client-abort); the thrown OperationCanceledException has already
                    // propagated and the enumerator surfaces the teardown.
                    eventCts.TryReset();
                }

                yield return result;
            }
        }
        finally
        {
            await eventCtsRegistration.DisposeAsync();
            eventCts?.Dispose();
        }
    }
}
