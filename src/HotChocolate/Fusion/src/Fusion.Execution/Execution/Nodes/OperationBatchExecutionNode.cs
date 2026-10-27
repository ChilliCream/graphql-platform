using System.Buffers;
using System.Collections.Immutable;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class OperationBatchExecutionNode : ExecutionNode
{
    private readonly OperationDefinition[] _operations;
    private readonly bool _requiresFileUpload;

    internal OperationBatchExecutionNode(
        int id,
        OperationDefinition[] operations)
    {
        Id = id;
        _operations = operations;
        SchemaName = operations[0].SchemaName!;
        _requiresFileUpload = operations.Any(t => t.RequiresFileUpload);
    }

    public override int Id { get; }

    public override ExecutionNodeType Type => ExecutionNodeType.OperationBatch;

    public override ReadOnlySpan<ExecutionNodeCondition> Conditions => [];

    public override string SchemaName { get; }

    internal ReadOnlySpan<OperationDefinition> Operations => _operations;

    protected override IDisposable? CreateScope(OperationPlanContext context)
        => context.DiagnosticEvents.ExecuteOperationBatchNode(context, this, SchemaName);

    protected override ValueTask<ExecutionStatus> OnExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
    {
        if (_operations.Length == 1)
        {
            return ExecuteSingleAsync(context, cancellationToken);
        }

        return ExecuteBatchAsync(context, cancellationToken);
    }

    private async ValueTask<ExecutionStatus> ExecuteSingleAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken)
    {
        var diagnosticEvents = context.DiagnosticEvents;
        var schemaName = SchemaName;
        var operation = _operations[0];

        var variables = CreateVariableValueSets(context, operation, out var reusedCount);

        if (variables.Length == 0
            && (operation.Requirements.Length > 0
                || operation.ForwardedVariables.Length > 0))
        {
            // When every entity was taken from the result, the node completed its work.
            return reusedCount > 0 ? ExecutionStatus.Success : ExecutionStatus.Skipped;
        }

        context.TrackVariableValueSets(this, variables);

        var sink = new SingleOperationResultSink(this, context, schemaName, operation, variables, cancellationToken);

        try
        {
            var operationType = operation.SourceText.Type;
            var client = context.GetClient(schemaName, operationType);

            if (client is HttpSourceSchemaClient httpClient
                && httpClient.CanExecuteBuffered(operationType, operation.RequiresFileUpload, variables.Length))
            {
                await httpClient
                    .ExecuteBufferedAsync(
                        context,
                        CreateRequest(schemaName, operation, variables),
                        sink,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await foreach (var result in client
                    .ExecuteAsync(context, CreateRequest(schemaName, operation, variables), cancellationToken)
                    .ConfigureAwait(false))
                {
                    if (!sink.OnResult(0, result))
                    {
                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return ExecutionStatus.Failed;
        }
        catch (Exception exception)
        {
            diagnosticEvents.SourceSchemaTransportError(context, this, schemaName, exception);
            context.AddErrors(exception, variables, operation.ResultSelectionSet);
            return ExecutionStatus.Failed;
        }

        if (sink.IsFailed)
        {
            return ExecutionStatus.Failed;
        }

        return sink.HasSomeErrors ? ExecutionStatus.PartialSuccess : ExecutionStatus.Success;
    }

    private SourceSchemaClientRequest CreateRequest(
        string schemaName,
        OperationDefinition operation,
        ImmutableArray<VariableValues> variables)
        => new()
        {
            Node = this,
            SchemaName = schemaName,
            OperationType = operation.SourceText.Type,
            OperationSourceText = operation.SourceText,
            Variables = variables,
            RequiresFileUpload = operation.RequiresFileUpload,
            OperationDocument = operation.Document,
            LookupTypeName = operation.LookupTypeName,
            ForwardedVariables = operation.GetForwardedVariablesArray()
        };

    private async ValueTask<ExecutionStatus> ExecuteBatchAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken)
    {
        var diagnosticEvents = context.DiagnosticEvents;
        var schemaName = SchemaName;

        var requestBuilder = ImmutableArray.CreateBuilder<SourceSchemaClientRequest>(_operations.Length);
        var operationByIndex = ArrayPool<OperationDefinition>.Shared.Rent(_operations.Length);
        var variablesByIndex = ArrayPool<ImmutableArray<VariableValues>>.Shared.Rent(_operations.Length);
        var receivedResults = ArrayPool<bool>.Shared.Rent(_operations.Length);
        var operationCount = 0;

        try
        {
            operationCount = BuildRequests(
                context,
                schemaName,
                requestBuilder,
                operationByIndex,
                variablesByIndex,
                out var hasReusedEntities);

            if (operationCount == 0)
            {
                // When every entity was taken from the result, the node completed its work.
                return hasReusedEntities ? ExecutionStatus.Success : ExecutionStatus.Skipped;
            }

            var requests = requestBuilder.DrainToImmutable();

            // Obtain a transport client for the source schema and stream the batch
            // response. As each individual result arrives, we queue its merge for
            // the executor loop so downstream nodes can consume the data after completion.
            var client = context.GetClient(schemaName, requests[0].OperationType);
            receivedResults.AsSpan(0, operationCount).Clear();

            var sink = new BatchResultSink(
                this,
                context,
                schemaName,
                operationByIndex,
                variablesByIndex,
                receivedResults,
                cancellationToken);

            if (client is HttpSourceSchemaClient httpClient && httpClient.CanExecuteBatchBuffered(requests))
            {
                await httpClient
                    .ExecuteBatchBufferedAsync(context, requests, sink, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await foreach (var batchResult in client
                    .ExecuteBatchAsync(context, requests, cancellationToken)
                    .ConfigureAwait(false))
                {
                    if (!sink.OnResult(batchResult.RequestIndex, batchResult.Result))
                    {
                        break;
                    }
                }
            }

            if (sink.IsCanceled)
            {
                return ExecutionStatus.Failed;
            }

            var overallStatus = sink.Status;

            // Verify that the downstream service returned a result for every
            // operation in the batch. A missing result means the service did
            // not implement the batch protocol correctly. We surface this as
            // an error so the issue is easy to diagnose.
            var missingCount = 0;

            for (var i = 0; i < operationCount; i++)
            {
                if (!receivedResults[i])
                {
                    missingCount++;
                    var operation = operationByIndex[i];

                    // A missing result is either a transport failure that the batch
                    // fallback isolated to this request, or a source schema that did
                    // not honor the batch protocol. When a transport failure was
                    // recorded we surface its cause; otherwise we report the missing
                    // batch result.
                    if (context.TryGetBatchRequestError(this, i, out var requestError))
                    {
                        diagnosticEvents.SourceSchemaTransportError(context, this, schemaName, requestError);
                        context.AddErrors(requestError, variablesByIndex[i], operation.ResultSelectionSet);
                    }
                    else
                    {
                        context.AddErrors(
                            ThrowHelper.MissingBatchResult(operation.Id),
                            variablesByIndex[i],
                            operation.ResultSelectionSet);
                    }
                }
            }

            if (missingCount > 0)
            {
                overallStatus = missingCount == operationCount
                    ? ExecutionStatus.Failed
                    : ExecutionStatus.PartialSuccess;
            }

            return overallStatus;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return ExecutionStatus.Failed;
        }
        catch (Exception exception)
        {
            diagnosticEvents.SourceSchemaTransportError(context, this, schemaName, exception);

            // The transport itself failed, so every operation in the batch is affected.
            // We attach the error to each operation's result selection set.
            for (var i = 0; i < operationCount; i++)
            {
                context.AddErrors(exception, variablesByIndex[i], operationByIndex[i].ResultSelectionSet);
            }

            return ExecutionStatus.Failed;
        }
        finally
        {
            operationByIndex.AsSpan(0, operationCount).Clear();
            variablesByIndex.AsSpan(0, operationCount).Clear();
            ArrayPool<OperationDefinition>.Shared.Return(operationByIndex);
            ArrayPool<ImmutableArray<VariableValues>>.Shared.Return(variablesByIndex);
            ArrayPool<bool>.Shared.Return(receivedResults);
        }
    }

    private int BuildRequests(
        OperationPlanContext context,
        string schemaName,
        ImmutableArray<SourceSchemaClientRequest>.Builder requestBuilder,
        OperationDefinition[] operationByIndex,
        ImmutableArray<VariableValues>[] variablesByIndex,
        out bool hasReusedEntities)
    {
        var operationCount = 0;
        hasReusedEntities = false;

        foreach (var operation in _operations)
        {
            if (IsSkipped(context, operation))
            {
                context.TrackSkippedDefinition(this, operation);
                continue;
            }

            if (HasSkippedDependencies(context, operation))
            {
                context.TrackSkippedDefinition(this, operation);
                continue;
            }

            var variables = CreateVariableValueSets(context, operation, out var reusedCount);

            if (reusedCount > 0)
            {
                hasReusedEntities = true;
            }

            if (variables.Length == 0
                && (operation.Requirements.Length > 0
                    || operation.ForwardedVariables.Length > 0))
            {
                // An operation whose entities were all taken from the result is not skipped.
                if (reusedCount == 0)
                {
                    context.TrackSkippedDefinition(this, operation);
                }

                continue;
            }

            context.TrackVariableValueSets(this, variables);

            requestBuilder.Add(new SourceSchemaClientRequest
            {
                Node = this,
                SchemaName = schemaName,
                OperationType = operation.SourceText.Type,
                OperationSourceText = operation.SourceText,
                Variables = variables,
                RequiresFileUpload = _requiresFileUpload,
                OperationDocument = operation.Document,
                LookupTypeName = operation.LookupTypeName,
                ForwardedVariables = operation.GetForwardedVariablesArray()
            });

            operationByIndex[operationCount] = operation;
            variablesByIndex[operationCount] = variables;
            operationCount++;
        }

        return operationCount;
    }

    protected override bool IsSkipped(OperationPlanContext context)
    {
        if (_operations.Length == 1)
        {
            return IsSkipped(context, _operations[0]);
        }

        if (_operations.Length == 2)
        {
            return IsSkipped(context, _operations[0])
                && IsSkipped(context, _operations[1]);
        }

        if (_operations.Length == 3)
        {
            return IsSkipped(context, _operations[0])
                && IsSkipped(context, _operations[1])
                && IsSkipped(context, _operations[2]);
        }

        if (_operations.Length == 4)
        {
            return IsSkipped(context, _operations[0])
                && IsSkipped(context, _operations[1])
                && IsSkipped(context, _operations[2])
                && IsSkipped(context, _operations[3]);
        }

        foreach (var operation in _operations)
        {
            if (!IsSkipped(context, operation))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsSkipped(OperationPlanContext context, OperationDefinition operation)
    {
        if (operation.Conditions.Length == 0)
        {
            return false;
        }

        foreach (var condition in operation.Conditions)
        {
            if (!context.Variables.TryGetValue<BooleanValueNode>(condition.VariableName, out var booleanValueNode))
            {
                throw ThrowHelper.MissingBooleanVariable(condition.VariableName);
            }

            if (booleanValueNode.Value != condition.PassingValue)
            {
                return true;
            }
        }

        return false;
    }

    protected override void OnSealingNode()
    {
        foreach (var operation in _operations)
        {
            operation.Seal();
        }
    }

    private static bool HasSkippedDependencies(OperationPlanContext context, OperationDefinition operation)
    {
        // A merged operation targets multiple result paths and each dependency
        // only feeds a subset of them, so it can still execute as long as at
        // least one dependency delivered data. Targets without data simply
        // produce no variable value sets.
        if (operation is BatchOperationDefinition)
        {
            if (operation.Dependencies.Length == 0)
            {
                return false;
            }

            foreach (var dep in operation.Dependencies)
            {
                if (!context.IsNodeSkipped(dep.Id))
                {
                    return false;
                }
            }

            return true;
        }

        foreach (var dep in operation.Dependencies)
        {
            if (context.IsNodeSkipped(dep.Id))
            {
                return true;
            }
        }

        return false;
    }

    private static ImmutableArray<VariableValues> CreateVariableValueSets(
        OperationPlanContext context,
        OperationDefinition operation,
        out int reusedCount)
    {
        reusedCount = 0;

        if (context.GetEntityReuseCandidates(operation.Id) is not { } candidates)
        {
            return CreateVariableValueSets(context, operation);
        }

        if (operation is SingleOperationDefinition single)
        {
            return context.CreateVariableValueSets(
                [single.Target],
                single.ForwardedVariables,
                single.Requirements,
                candidates,
                out reusedCount);
        }

        if (operation is BatchOperationDefinition batch)
        {
            return context.CreateVariableValueSets(
                batch.Targets,
                batch.ForwardedVariables,
                batch.Requirements,
                candidates,
                out reusedCount);
        }

        return CreateVariableValueSets(context, operation);
    }

    private static ImmutableArray<VariableValues> CreateVariableValueSets(
        OperationPlanContext context,
        OperationDefinition operation)
        => operation switch
        {
            SingleOperationDefinition single
                => context.CreateVariableValueSets(
                    single.Target,
                    single.ForwardedVariables,
                    single.Requirements),
            BatchOperationDefinition batch
                => context.CreateVariableValueSets(
                    batch.Targets,
                    batch.ForwardedVariables,
                    batch.Requirements),
            _ => throw new InvalidOperationException(
                $"Unknown operation definition type: {operation.GetType().Name}")
        };

    /// <summary>
    /// Queues the merge of each result of a single operation as it arrives and stops reading at the
    /// first result that cannot be queued.
    /// </summary>
    private sealed class SingleOperationResultSink(
        OperationBatchExecutionNode node,
        OperationPlanContext context,
        string schemaName,
        OperationDefinition operation,
        ImmutableArray<VariableValues> variables,
        CancellationToken cancellationToken)
        : ISourceSchemaResultSink
    {
        /// <summary>
        /// Gets whether any received result carries errors.
        /// </summary>
        public bool HasSomeErrors { get; private set; }

        /// <summary>
        /// Gets whether a result could not be queued, which fails the node.
        /// </summary>
        public bool IsFailed { get; private set; }

        public bool OnResult(int requestIndex, SourceSchemaResult result)
        {
            var hasErrors = result.Errors is not null;
            if (hasErrors)
            {
                HasSomeErrors = true;
            }

            var pendingMerge = default(PendingMerge);
            var hasPendingMerge = false;

            try
            {
                pendingMerge = PendingMerge.Single(
                    node,
                    schemaName,
                    operation.Source,
                    operation.ResultSelectionSet,
                    variables,
                    result,
                    hasErrors);
                hasPendingMerge = true;
                context.EnqueuePendingMerge(pendingMerge);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (hasPendingMerge)
                {
                    pendingMerge.DisposeUnmerged();
                }

                IsFailed = true;
                return false;
            }
            catch (Exception exception)
            {
                if (hasPendingMerge)
                {
                    pendingMerge.DisposeUnmerged();
                }
                else
                {
                    result.Dispose();
                }

                context.DiagnosticEvents.SourceSchemaStoreError(context, node, schemaName, exception);
                context.AddErrors(exception, variables, operation.ResultSelectionSet);
                IsFailed = true;
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Queues the merge of each result of a batch as it arrives and records which operations
    /// received a result. A result that cannot be queued fails its operation but not the batch.
    /// </summary>
    private sealed class BatchResultSink(
        OperationBatchExecutionNode node,
        OperationPlanContext context,
        string schemaName,
        OperationDefinition[] operationByIndex,
        ImmutableArray<VariableValues>[] variablesByIndex,
        bool[] receivedResults,
        CancellationToken cancellationToken)
        : ISourceSchemaResultSink
    {
        /// <summary>
        /// Gets the status of the batch from the results received so far.
        /// </summary>
        public ExecutionStatus Status { get; private set; } = ExecutionStatus.Success;

        /// <summary>
        /// Gets whether queueing a result was canceled, which stops reading the batch.
        /// </summary>
        public bool IsCanceled { get; private set; }

        public bool OnResult(int requestIndex, SourceSchemaResult result)
        {
            var op = operationByIndex[requestIndex];
            var hasErrors = result.Errors is not null;

            receivedResults[requestIndex] = true;

            var pendingMerge = default(PendingMerge);
            var hasPendingMerge = false;

            try
            {
                pendingMerge = PendingMerge.Single(
                    node,
                    schemaName,
                    op.Source,
                    op.ResultSelectionSet,
                    variablesByIndex[requestIndex],
                    result,
                    hasErrors);
                hasPendingMerge = true;
                context.EnqueuePendingMerge(pendingMerge);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (hasPendingMerge)
                {
                    pendingMerge.DisposeUnmerged();
                }

                IsCanceled = true;
                return false;
            }
            catch (Exception exception)
            {
                if (hasPendingMerge)
                {
                    pendingMerge.DisposeUnmerged();
                }
                else
                {
                    result.Dispose();
                }

                context.DiagnosticEvents.SourceSchemaStoreError(context, node, schemaName, exception);
                context.AddErrors(exception, variablesByIndex[requestIndex], op.ResultSelectionSet);
                Status = ExecutionStatus.Failed;
                return true;
            }

            if (hasErrors && Status == ExecutionStatus.Success)
            {
                Status = ExecutionStatus.PartialSuccess;
            }

            return true;
        }
    }
}
