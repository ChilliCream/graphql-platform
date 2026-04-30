using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Results;
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

            // Complete the initial result while retaining data needed by active
            // incremental plans.
            var initialResult = context.Complete(retainMemoryForDefer: true);

            // Compute the active delivery groups (one per @defer occurrence whose
            // @defer(if:) evaluates to true) and the incremental plans that will actually run.
            // An incremental plan is active if at least one of its delivery groups is active.
            var activeDeliveryGroupIds = new HashSet<int>();
            foreach (var deliveryGroup in operationPlan.DeliveryGroups)
            {
                if (IsDeliveryGroupActive(deliveryGroup, variables))
                {
                    activeDeliveryGroupIds.Add(deliveryGroup.Id);
                }
            }

            // Mark top-level active delivery groups as pending on the initial
            // result. Nested delivery groups are marked pending after their
            // parent incremental plan completes.
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
                // No active top-level delivery groups. Transfer retained
                // result resources to the initial result.
                context.TransferRetainedMemoryTo(initialResult);
                executionCts.Dispose();
                await context.DisposeAsync();
                return initialResult;
            }

            // Return a ResponseStream that yields the initial result followed
            // by incremental results.
            var rootContext = context;
            var stream = new ResponseStream(
                () => CreateIncrementalStream(
                    requestContext,
                    variables,
                    operationPlan,
                    initialResult,
                    activeDeliveryGroupIds,
                    rootContext,
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

    private static async IAsyncEnumerable<OperationResult> CreateIncrementalStream(
        RequestContext requestContext,
        IVariableValueCollection variables,
        OperationPlan operationPlan,
        OperationResult initialResult,
        HashSet<int> activeDeliveryGroupIds,
        OperationPlanContext rootContext,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Yield the initial result first.
        yield return initialResult;

        var incrementalPlans = operationPlan.IncrementalPlans;

        // Per-delivery-group completion tracking. A delivery group is considered
        // complete when every incremental plan whose DeliveryGroups contains it has
        // finished. We also track which incremental plans are "active" so an inactive
        // @defer(if: false) group does not block completion accounting.
        var pendingCountByDeliveryGroup = new Dictionary<int, int>();
        foreach (var incrementalPlan in incrementalPlans)
        {
            if (!IsIncrementalPlanActive(incrementalPlan, activeDeliveryGroupIds))
            {
                continue;
            }

            foreach (var deliveryGroup in incrementalPlan.DeliveryGroups)
            {
                if (!activeDeliveryGroupIds.Contains(deliveryGroup.Id))
                {
                    continue;
                }

                pendingCountByDeliveryGroup[deliveryGroup.Id] =
                    pendingCountByDeliveryGroup.GetValueOrDefault(deliveryGroup.Id) + 1;
            }
        }

        // Active top-level incremental plans start immediately. Nested
        // incremental plans start when their parent delivery group has completed.
        //
        // Keep completed parent contexts available while nested incremental
        // plans are starting. Stored contexts are disposed when the iterator
        // finishes.
        var started = new HashSet<IncrementalPlan>();
        var incrementalPlanContexts = new Dictionary<IncrementalPlan, OperationPlanContext>();
        var channel = Channel.CreateUnbounded<IncrementalPlanResult>();
        var pendingIncrementalPlanCount = 0;

        try
        {
            foreach (var incrementalPlan in incrementalPlans)
            {
                if (!IsIncrementalPlanActive(incrementalPlan, activeDeliveryGroupIds))
                {
                    continue;
                }

                if (incrementalPlan.DeliveryGroups[0].Parent is not null)
                {
                    continue;
                }

                started.Add(incrementalPlan);
                pendingIncrementalPlanCount++;
                BeginIncrementalPlan(
                    requestContext,
                    variables,
                    incrementalPlan,
                    rootContext.GetResultStoreForChildDefer(),
                    channel.Writer,
                    cancellationToken);
            }

            // Track which delivery groups we have already announced as pending so
            // we do not re-announce nested groups multiple times when they belong
            // to more than one incremental plan.
            var announcedDeliveryGroupIds = new HashSet<int>();
            foreach (var deliveryGroup in operationPlan.DeliveryGroups)
            {
                if (deliveryGroup.Parent is null && activeDeliveryGroupIds.Contains(deliveryGroup.Id))
                {
                    announcedDeliveryGroupIds.Add(deliveryGroup.Id);
                }
            }

            // Yield results as they complete.
            while (pendingIncrementalPlanCount > 0 && !cancellationToken.IsCancellationRequested)
            {
                var (incrementalPlan, incrementalPlanContext, result, error) = await channel.Reader.ReadAsync(cancellationToken);
                pendingIncrementalPlanCount--;

                // Register the completed incremental plan's context so any nested
                // defer launched below can source its parent store from this
                // immediately enclosing incremental plan rather than the root plan.
                if (incrementalPlanContext is not null)
                {
                    incrementalPlanContexts[incrementalPlan] = incrementalPlanContext;
                }

                // Start nested incremental plans whose parent delivery group
                // belongs to the just-completed incremental plan, then mark
                // their delivery groups as pending on the outgoing result.
                var childPending = ImmutableList.CreateBuilder<PendingResult>();
                foreach (var candidate in incrementalPlans)
                {
                    if (!IsIncrementalPlanActive(candidate, activeDeliveryGroupIds))
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
                    foreach (var deliveryGroup in incrementalPlan.DeliveryGroups)
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

                    // Nested plans resolve requirements from the immediate
                    // enclosing plan when available.
                    var parentStore = incrementalPlanContext?.GetResultStoreForChildDefer()
                        ?? rootContext.GetResultStoreForChildDefer();

                    started.Add(candidate);
                    pendingIncrementalPlanCount++;
                    BeginIncrementalPlan(
                        requestContext,
                        variables,
                        candidate,
                        parentStore,
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

                // Use the deepest delivery group path for this incremental
                // plan. Ties are broken by the smallest DeliveryGroup.Id.
                var bestDeliveryGroup = PickBestDeliveryGroup(incrementalPlan);

                // Build the result for this incremental plan and update
                // delivery group completion state.
                var completed = ImmutableList.CreateBuilder<CompletedResult>();
                OperationResult payload;

                if (error is not null)
                {
                    var errorObj = ErrorBuilder.New()
                        .SetMessage(error.Message)
                        .Build();
                    payload = OperationResult.FromError(errorObj);
                    CompleteDeliveryGroupsForIncrementalPlan(
                        incrementalPlan,
                        activeDeliveryGroupIds,
                        pendingCountByDeliveryGroup,
                        completed,
                        errors: [errorObj]);
                }
                else if (result is not null)
                {
                    payload = result;

                    // Use the selected delivery group's path to produce the
                    // incremental result data for this plan.
                    if (result.Data.HasValue
                        && !result.Data.Value.IsValueNull
                        && TryCreateIncrementalResults(
                            result.Data.Value,
                            bestDeliveryGroup,
                            result.Errors.Count > 0 ? result.Errors : null,
                            out var incrementalResults))
                    {
                        payload.Incremental = incrementalResults;
                    }

                    CompleteDeliveryGroupsForIncrementalPlan(
                        incrementalPlan,
                        activeDeliveryGroupIds,
                        pendingCountByDeliveryGroup,
                        completed,
                        errors: result.Errors.Count > 0 && payload.Incremental.Count == 0
                            ? result.Errors
                            : null);
                }
                else
                {
                    // Incremental plan with no execution result: all fields may
                    // have been conditional and excluded. Report a successful
                    // completion with no data.
                    var placeholder = ErrorBuilder.New()
                        .SetMessage("placeholder")
                        .Build();
                    payload = OperationResult.FromError(placeholder);
                    CompleteDeliveryGroupsForIncrementalPlan(
                        incrementalPlan,
                        activeDeliveryGroupIds,
                        pendingCountByDeliveryGroup,
                        completed,
                        errors: null);
                }

                // Set Completed before clearing top-level errors.
                if (completed.Count > 0)
                {
                    payload.Completed = completed.ToImmutable();
                }

                if (childPending.Count > 0)
                {
                    payload.Pending = childPending.ToImmutable();
                }

                // Incremental results do not carry root data or top-level errors.
                payload.Data = null;
                if (payload.Errors.Count > 0)
                {
                    payload.Errors = [];
                }

                payload.HasNext = pendingIncrementalPlanCount > 0;
                yield return payload;
            }
        }
        finally
        {
            // Dispose completed incremental plan contexts after the stream
            // finishes. The root context is owned by the surrounding stream.
            foreach (var incrementalPlanContext in incrementalPlanContexts.Values)
            {
                await incrementalPlanContext.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Starts an incremental plan and publishes completion to the channel.
    /// Plans without execution nodes complete without running work.
    /// </summary>
    private static void BeginIncrementalPlan(
        RequestContext requestContext,
        IVariableValueCollection variables,
        IncrementalPlan incrementalPlan,
        FetchResultStore parentResultStore,
        ChannelWriter<IncrementalPlanResult> completion,
        CancellationToken cancellationToken)
    {
        if (incrementalPlan.AllNodes.IsEmpty)
        {
            completion.TryWrite(new IncrementalPlanResult(incrementalPlan, null, null, null));
            return;
        }

        var executionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var context = requestContext.Schema.Services.GetRequiredService<OperationPlanContextPool>().Rent();

        try
        {
            context.Initialize(requestContext, variables, incrementalPlan, executionCts);

            // Copy parent-scope requirements into the child context.
            CollectIncrementalPlanRequirements(parentResultStore, incrementalPlan, context);
        }
        catch
        {
            _ = context.DisposeAsync();
            executionCts.Dispose();
            throw;
        }

        // Ownership of context and executionCts passes to ExecuteIncrementalPlan.
        _ = ExecuteIncrementalPlan(context, incrementalPlan, executionCts, completion, cancellationToken);
    }

    private static async Task ExecuteIncrementalPlan(
        OperationPlanContext context,
        IOperationPlan plan,
        CancellationTokenSource executionCts,
        ChannelWriter<IncrementalPlanResult> completion,
        CancellationToken cancellationToken)
    {
        var incrementalPlan = (IncrementalPlan)plan;

        try
        {
            context.Begin();

            await ExecuteQueryAsync(context, plan, executionCts.Token);

            // Keep result resources available for nested incremental plans.
            var deferredResult = context.Complete(retainMemoryForDefer: true);
            await completion.WriteAsync(
                new IncrementalPlanResult(incrementalPlan, context, deferredResult, null),
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Signal cancellation so the consumer's counter balances; the
            // context is handed back on the channel so the iterator can
            // dispose it as part of its finally-block cleanup.
            await completion.WriteAsync(
                new IncrementalPlanResult(incrementalPlan, context, null, null),
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            await completion.WriteAsync(
                new IncrementalPlanResult(incrementalPlan, context, null, ex),
                CancellationToken.None);
        }
        finally
        {
            executionCts.Dispose();
        }
    }

    /// <summary>
    /// Collects requirements satisfied by the enclosing plan scope and
    /// installs their values on the child context.
    /// </summary>
    private static void CollectIncrementalPlanRequirements(
        FetchResultStore parentResultStore,
        IncrementalPlan incrementalPlan,
        OperationPlanContext childContext)
    {
        var collected = CollectParentScopeRequirements(incrementalPlan);
        if (collected.Count == 0)
        {
            return;
        }

        var anchor = collected[0].Path;
        for (var i = 1; i < collected.Count; i++)
        {
            if (!collected[i].Path.Equals(anchor))
            {
                throw new InvalidOperationException(
                    "Deferred incremental plan has parent-sourced requirements at different anchor paths; "
                    + "one-time materialization assumes a single anchor.");
            }
        }

        var requirementSpan = new OperationRequirement[collected.Count];
        var keys = new HashSet<string>(collected.Count, StringComparer.Ordinal);
        for (var i = 0; i < collected.Count; i++)
        {
            requirementSpan[i] = collected[i];
            keys.Add(collected[i].Key);
        }

        var parentValues = parentResultStore.CreateVariableValueSets(
            anchor,
            requestVariables: [],
            requirementSpan);

        childContext.SetRequirements(parentValues, keys);
    }

    private static List<OperationRequirement> CollectParentScopeRequirements(IncrementalPlan incrementalPlan)
    {
        var collected = new List<OperationRequirement>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var node in incrementalPlan.AllNodes)
        {
            switch (node)
            {
                case OperationExecutionNode op when !op.ParentDependencies.IsEmpty:
                    AppendRequirements(op.Requirements, collected, seen);
                    break;

                case OperationBatchExecutionNode batch:
                    foreach (var definition in batch.Operations)
                    {
                        if (!definition.ParentDependencies.IsEmpty)
                        {
                            AppendRequirements(definition.Requirements, collected, seen);
                        }
                    }
                    break;
            }
        }

        return collected;
    }

    private static void AppendRequirements(
        ReadOnlySpan<OperationRequirement> requirements,
        List<OperationRequirement> collected,
        HashSet<string> seen)
    {
        foreach (var requirement in requirements)
        {
            if (seen.Add(requirement.Key))
            {
                collected.Add(requirement);
            }
        }
    }

    private static bool IsDeliveryGroupActive(DeliveryGroup deliveryGroup, IVariableValueCollection variables)
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

    private static bool IsIncrementalPlanActive(IncrementalPlan incrementalPlan, HashSet<int> activeDeliveryGroupIds)
    {
        foreach (var deliveryGroup in incrementalPlan.DeliveryGroups)
        {
            if (activeDeliveryGroupIds.Contains(deliveryGroup.Id))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Selects the delivery group used to anchor an incremental result.
    /// The group with the deepest path is selected.
    /// Ties are broken by the smallest <see cref="DeliveryGroup.Id"/>.
    /// </summary>
    private static DeliveryGroup PickBestDeliveryGroup(IncrementalPlan incrementalPlan)
    {
        var best = incrementalPlan.DeliveryGroups[0];
        var bestLength = best.Path?.Length ?? 0;

        for (var i = 1; i < incrementalPlan.DeliveryGroups.Length; i++)
        {
            var candidate = incrementalPlan.DeliveryGroups[i];
            var candidateLength = candidate.Path?.Length ?? 0;

            if (candidateLength > bestLength)
            {
                best = candidate;
                bestLength = candidateLength;
            }
        }

        return best;
    }

    private static void CompleteDeliveryGroupsForIncrementalPlan(
        IncrementalPlan incrementalPlan,
        HashSet<int> activeDeliveryGroupIds,
        Dictionary<int, int> pendingCountByDeliveryGroup,
        ImmutableList<CompletedResult>.Builder completed,
        IReadOnlyList<IError>? errors)
    {
        foreach (var deliveryGroup in incrementalPlan.DeliveryGroups)
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
    /// Produces <see cref="IncrementalObjectResult"/> entries whose logical root
    /// is the subtree at the best delivery group's path within the deferred
    /// plan's composite result. The incremental delivery contract requires
    /// <c>incremental.data</c> to be a map of fields to merge at the pending
    /// path, not the fully rooted result. When the pending path points at a list,
    /// each list element is emitted as a separate incremental result with a
    /// relative index <c>subPath</c>.
    /// </summary>
    private static bool TryCreateIncrementalResults(
        OperationResultData rootData,
        DeliveryGroup bestDeliveryGroup,
        ImmutableList<IError>? errors,
        out ImmutableList<IIncrementalResult> incrementalResults)
    {
        if (rootData.Value is not CompositeResultDocument document)
        {
            // Unknown backing value: fall through to the default behavior and
            // emit the result as-is.
            incrementalResults =
            [
                new IncrementalObjectResult(
                    bestDeliveryGroup.Id,
                    errors,
                    data: rootData)
            ];
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
                incrementalResults = [];
                return false;
            }

            element = next;
        }

        if (element.ValueKind is JsonValueKind.Array)
        {
            var builder = ImmutableList.CreateBuilder<IIncrementalResult>();
            var length = element.GetArrayLength();

            for (var i = 0; i < length; i++)
            {
                var item = element[i];

                if (item.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                {
                    continue;
                }

                builder.Add(
                    new IncrementalObjectResult(
                        bestDeliveryGroup.Id,
                        errors,
                        subPath: Path.Root.Append(i),
                        data: CreateIncrementalData(document, item)));
            }

            incrementalResults = builder.ToImmutable();
            return true;
        }

        incrementalResults =
        [
            new IncrementalObjectResult(
                bestDeliveryGroup.Id,
                errors,
                data: CreateIncrementalData(document, element))
        ];
        return true;
    }

    private static OperationResultData CreateIncrementalData(
        CompositeResultDocument document,
        CompositeResultElement element)
        // MemoryHolder is intentionally not carried over. The surrounding
        // OperationResult already owns the composite document's lifetime, and
        // the IncrementalObjectResult is a non-owning view over it.
        => new(
            document,
            isValueNull: false,
            new DeferredPayloadDataFormatter(element),
            memoryHolder: null);

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
        IOperationPlan plan,
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
        IOperationPlan plan,
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

internal readonly record struct IncrementalPlanResult(
    IncrementalPlan IncrementalPlan,
    OperationPlanContext? Context,
    OperationResult? Result,
    Exception? Error);
