using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using HotChocolate.Buffers;
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
        OperationPlanContext? rootContext = null;

        try
        {
            rootContext = requestContext.Schema.Services.GetRequiredService<OperationPlanContextPool>().Rent();
            rootContext.Initialize(requestContext, variables, operationPlan, executionCts);

            rootContext.Begin();

            switch (operationPlan.Operation.Definition.Operation)
            {
                case OperationType.Query:
                    await ExecuteQueryAsync(rootContext, operationPlan, executionCts.Token);
                    break;

                case OperationType.Mutation:
                    await ExecuteMutationAsync(rootContext, operationPlan, executionCts.Token);
                    break;

                default:
                    throw new InvalidOperationException("Only queries and mutations can use @defer.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Complete the initial result while retaining data needed by active
            // incremental plans.
            var initialResult = rootContext.Complete(retainMemoryForDefer: true);

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
                rootContext.TransferRetainedMemoryTo(initialResult);
                executionCts.Dispose();
                await rootContext.DisposeAsync();
                return initialResult;
            }

            // Capture the single request arena now. The request executor detaches it from the
            // request context once this method returns the stream, so every incremental plan reuses
            // the captured arena instead of minting its own, and the stream seals it once when read
            // to completion.
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

            stream.RegisterForCleanup(rootContext);
            stream.RegisterForCleanup(executionCts);
            return stream;
        }
        catch (Exception)
        {
            executionCts.Dispose();

            if (rootContext is not null)
            {
                await rootContext.DisposeAsync();
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

        var requestArena = rootContext.Memory;
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
                    requestArena,
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
                        requestArena,
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

            // The stream was read to completion, so the initial plan and every incremental plan have
            // finished writing into the single request arena.
            //
            // Sealing it here lets its pages be  returned to the pool when the stream result is disposed.
            // On early disposal or cancellation we never reach this point and the arena is abandoned instead,
            // because an in-flight incremental plan may still write to it.
            requestArena.Seal();
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
        MemoryArena requestArena,
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
            // Reuse the single request arena instead of minting a per-plan arena: the request
            // executor has already detached it from the request context, so it is supplied
            // explicitly here.
            context.Initialize(requestContext, variables, incrementalPlan, executionCts, requestArena);

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

        // We create a new CancellationTokenSource that can be used to halt the execution engine,
        // without also cancelling the entire request pipeline.
        var executionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        OperationPlanContext? context = null;
        CancellationTokenRegistration? cancellationRegistration = null;

        try
        {
            context = requestContext.Schema.Services.GetRequiredService<OperationPlanContextPool>().Rent();
            context.Initialize(requestContext, requestContext.VariableValues[0], operationPlan, executionCts);

            var subscriptionResult = root switch
            {
                OperationExecutionNode subscriptionNode => subscriptionNode.Subscribe(context),
                EventStreamExecutionNode eventStreamNode => eventStreamNode.Subscribe(context),
                _ => throw new InvalidOperationException("The specified operation plan is not supported.")
            };
            var executionState = context.ExecutionState;

            cancellationRegistration = executionCts.Token.Register(
                static state => Unsafe.As<AsyncAutoResetEvent>(state)!.TryResetToIdle(),
                executionState.Signal);

            if (subscriptionResult.Status is not ExecutionStatus.Success)
            {
                throw new InvalidOperationException("We could not subscribe to the underlying source schema.");
            }

            // The subscription setup is complete and nothing writes into the subscribe-scoped request
            // arena anymore; each event rents its own arena. Sealing it here lets its pages be returned
            // to the pool once the subscription result is disposed. If the setup fails we never get here
            // and the unsealed arena is abandoned instead.
            requestContext.Memory?.Seal();

            var subscriptionEnumerable = CreateResponseStream(
                context,
                root,
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
            while (executionState.TryDequeuePendingMerge(out var merge))
            {
                executionState.ApplyMerge(context, merge);
            }

            while (executionState.TryDequeueCompletedResult(out var result))
            {
                while (executionState.TryDequeuePendingMerge(out var merge))
                {
                    executionState.ApplyMerge(context, merge);
                }

                result = executionState.ApplyPendingMergeFailure(result);
                var node = plan.GetNodeById(result.Id);
                executionState.CompleteNode(plan, node, result);
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                executionState.EnqueueNextNodes(context, cancellationToken);
            }

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
                while (executionState.TryDequeuePendingMerge(out var merge))
                {
                    executionState.ApplyMerge(context, merge);
                }

                while (executionState.TryDequeueCompletedResult(out var result))
                {
                    while (executionState.TryDequeuePendingMerge(out var merge))
                    {
                        executionState.ApplyMerge(context, merge);
                    }

                    result = executionState.ApplyPendingMergeFailure(result);
                    var node = plan.GetNodeById(result.Id);
                    executionState.CompleteNode(plan, node, result);
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    executionState.EnqueueNextNodes(context, cancellationToken);
                }

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
        ExecutionNode subscriptionNode,
        SubscriptionResult subscriptionResult,
        ExecutionConcurrencyGate? concurrencyGate,
        TimeSpan eventTimeout,
        [EnumeratorCancellation] CancellationToken executionCancellationToken,
        CancellationToken requestCancellationToken)
    {
        var plan = context.OperationPlan;
        var executionState = context.ExecutionState;

        await using var cancellationRegistration = executionCancellationToken.Register(
            static state => Unsafe.As<AsyncAutoResetEvent>(state)!.TryResetToIdle(),
            executionState.Signal);

        // We allocate a single CancellationTokenSource per subscription and reuse it
        // across all events via TryReset(). The execution token is linked in so that
        // client-abort / server-shutdown still propagates.
        // if a cancellation is requested because of null-propagation to the root,
        // we will reset the source and continue to the next event.
        var (eventCts, eventCtsRegistration) = CreateEventCancellation();

        var schemaName = GetSubscriptionSchemaName(context, subscriptionNode);

        try
        {
            await foreach (var eventArgs in subscriptionResult.ReadStreamAsync()
                .WithCancellation(executionCancellationToken))
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
                            DependentsToExecute: eventArgs.DependentsToExecute,
                            SkippedDefinitions: [],
                            VariableValueSets: eventArgs.VariableValueSets));

                    while (!eventToken.IsCancellationRequested && executionState.IsProcessing())
                    {
                        while (executionState.TryDequeuePendingMerge(out var merge))
                        {
                            executionState.ApplyMerge(context, merge);
                        }

                        while (executionState.TryDequeueCompletedResult(out var nodeResult))
                        {
                            while (executionState.TryDequeuePendingMerge(out var merge))
                            {
                                executionState.ApplyMerge(context, merge);
                            }

                            nodeResult = executionState.ApplyPendingMergeFailure(nodeResult);
                            var node = plan.GetNodeById(nodeResult.Id);
                            executionState.CompleteNode(plan, node, nodeResult);
                        }

                        if (!eventToken.IsCancellationRequested)
                        {
                            executionState.EnqueueNextNodes(context, eventToken);
                        }

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

                    // If the event token was cancelled by a genuine timeout or abort, tear the
                    // stream down. A root-null halt also cancels the event token, but that is benign
                    // (the result is a settled {data: null, errors: [...]}), so we keep the stream
                    // alive and let context.Complete() produce it.
                    if (!executionState.ProcessingCompletedEarly)
                    {
                        eventToken.ThrowIfCancellationRequested();
                    }

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

                    if (executionState.ProcessingCompletedEarly)
                    {
                        // A root-null halt cancelled the event source. A cancelled source cannot be
                        // reset, so swap in a fresh one (and re-link client-abort / shutdown) for the
                        // next event. This only happens on events that null-propagate to the root.
                        await eventCtsRegistration.DisposeAsync();
                        eventCts.Dispose();
                        (eventCts, eventCtsRegistration) = CreateEventCancellation();
                    }
                    else
                    {
                        // Healthy event: reset the shared source so the next event starts with a
                        // fresh timeout budget and no allocation. If TryReset() returns false the
                        // source was cancelled by a timeout or client-abort; the thrown
                        // OperationCanceledException has already propagated and the enumerator
                        // surfaces the teardown.
                        eventCts.TryReset();
                    }
                }

                yield return result;
            }
        }
        finally
        {
            await eventCtsRegistration.DisposeAsync();
            eventCts?.Dispose();
        }

        // Creates a fresh event-scoped cancellation source, links client-abort / shutdown into it,
        // and installs it as the engine's cancellation source for the next event.
        (CancellationTokenSource Source, CancellationTokenRegistration Registration) CreateEventCancellation()
        {
            var cts = new CancellationTokenSource();
            var registration = executionCancellationToken.UnsafeRegister(
                static state => Unsafe.As<CancellationTokenSource>(state)!.Cancel(),
                cts);
            executionState.SetCancellationSource(cts);
            return (cts, registration);
        }
    }

    private static string GetSubscriptionSchemaName(
        OperationPlanContext context,
        ExecutionNode subscriptionNode)
        => subscriptionNode.SchemaName
            ?? (subscriptionNode is EventStreamExecutionNode ? "event-stream" : context.GetDynamicSchemaName(subscriptionNode));
}

internal readonly record struct IncrementalPlanResult(
    IncrementalPlan IncrementalPlan,
    OperationPlanContext? Context,
    OperationResult? Result,
    Exception? Error);
