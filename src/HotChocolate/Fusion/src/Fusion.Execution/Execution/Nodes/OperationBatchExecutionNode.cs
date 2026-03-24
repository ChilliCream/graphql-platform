using System.Buffers;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class OperationBatchExecutionNode : ExecutionNode
{
    private readonly OperationDefinition[] _operations;

    internal OperationBatchExecutionNode(
        int id,
        OperationDefinition[] operations)
    {
        Id = id;
        _operations = operations;
        SchemaName = operations[0].SchemaName!;
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
        // When the batch holds a single non-merged operation, the planner
        // promotes all of its dependencies onto the batch node as required.
        // So if we reach this point, every dependency has already succeeded.
        // We use the simpler ExecuteAsync path which avoids the batch
        // streaming infrastructure (no lists, no receivedResults tracking).
        // Note: BatchOperationDefinition (merged multi-target ops) uses the
        // batch path because its deps are optional: some targets' deps may
        // be skipped while others succeed.
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

        var variables = CreateVariableValueSets(context, operation);

        if (variables.Length == 0
            && (operation.Requirements.Length > 0
                || operation.ForwardedVariables.Length > 0))
        {
            return ExecutionStatus.Skipped;
        }

        context.TrackVariableValueSets(this, variables);

        var request = new SourceSchemaClientRequest
        {
            Node = this,
            SchemaName = schemaName,
            OperationType = operation.Operation.Type,
            OperationSourceText = operation.Operation.SourceText,
            Variables = variables,
            RequiresFileUpload = operation.RequiresFileUpload
        };

        var index = 0;
        var bufferLength = 0;
        SourceSchemaResult[]? buffer = null;
        SourceSchemaResult? singleResult = null;
        var hasSomeErrors = false;

        try
        {
            var client = context.GetClient(schemaName, operation.Operation.Type);
            var response = await client.ExecuteAsync(context, request, cancellationToken).ConfigureAwait(false);
            context.TrackSourceSchemaClientResponse(this, response);

            var totalPathCount = variables.Length;

            for (var i = 0; i < variables.Length; i++)
            {
                totalPathCount += variables[i].AdditionalPaths.Length;
            }

            var initialBufferLength = Math.Max(totalPathCount, 4);

            await foreach (var result in response.ReadAsResultStreamAsync(cancellationToken).ConfigureAwait(false))
            {
                if (index == 0)
                {
                    singleResult = result;
                    index = 1;
                }
                else
                {
                    if (buffer is null)
                    {
                        bufferLength = initialBufferLength;
                        buffer = ArrayPool<SourceSchemaResult>.Shared.Rent(bufferLength);
                        buffer[0] = singleResult!;
                    }

                    buffer[index++] = result;
                }

                if (result.Errors is not null)
                {
                    hasSomeErrors = true;
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

            if (buffer is not null && bufferLength > 0)
            {
                foreach (var result in buffer.AsSpan(0, index))
                {
                    result?.Dispose();
                }

                buffer.AsSpan(0, index).Clear();
                ArrayPool<SourceSchemaResult>.Shared.Return(buffer);
            }
            else if (singleResult is not null)
            {
                singleResult.Dispose();
            }

            context.AddErrors(exception, variables, operation.ResultSelectionSet);
            return ExecutionStatus.Failed;
        }

        try
        {
            if (buffer is not null)
            {
                context.AddPartialResults(
                    operation.Source,
                    buffer.AsSpan(0, index),
                    operation.ResultSelectionSet,
                    hasSomeErrors);
            }
            else if (singleResult is not null)
            {
                var firstResult = singleResult;
                context.AddPartialResults(
                    operation.Source,
                    MemoryMarshal.CreateReadOnlySpan(ref firstResult, 1),
                    operation.ResultSelectionSet,
                    hasSomeErrors);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return ExecutionStatus.Failed;
        }
        catch (Exception exception)
        {
            diagnosticEvents.SourceSchemaStoreError(context, this, schemaName, exception);
            context.AddErrors(exception, variables, operation.ResultSelectionSet);
            return ExecutionStatus.Failed;
        }
        finally
        {
            if (buffer is not null)
            {
                buffer.AsSpan(0, index).Clear();
                ArrayPool<SourceSchemaResult>.Shared.Return(buffer);
            }
        }

        return hasSomeErrors ? ExecutionStatus.PartialSuccess : ExecutionStatus.Success;
    }

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
            operationCount = BuildRequests(context, schemaName, requestBuilder, operationByIndex, variablesByIndex);

            if (operationCount == 0)
            {
                return ExecutionStatus.Skipped;
            }

            var requests = requestBuilder.DrainToImmutable();

            // Obtain a transport client for the source schema and stream the batch
            // response. As each individual result arrives, we merge it into the
            // result store so downstream nodes can consume the data.
            var client = context.GetClient(schemaName, requests[0].OperationType);
            receivedResults.AsSpan(0, operationCount).Clear();
            var overallStatus = ExecutionStatus.Success;

            await foreach (var batchResult in client.ExecuteBatchStreamAsync(context, requests, cancellationToken))
            {
                var requestIndex = batchResult.RequestIndex;
                var op = operationByIndex[requestIndex];
                var result = batchResult.Result;
                var hasErrors = result.Errors is not null;

                receivedResults[requestIndex] = true;

                try
                {
                    context.AddPartialResult(
                        op.Source,
                        result,
                        op.ResultSelectionSet,
                        hasErrors);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return ExecutionStatus.Failed;
                }
                catch (Exception exception)
                {
                    diagnosticEvents.SourceSchemaStoreError(context, this, schemaName, exception);
                    context.AddErrors(exception, variablesByIndex[requestIndex], op.ResultSelectionSet);
                    overallStatus = ExecutionStatus.Failed;
                    continue;
                }

                if (hasErrors && overallStatus == ExecutionStatus.Success)
                {
                    overallStatus = ExecutionStatus.PartialSuccess;
                }
            }

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
                    context.AddErrors(
                        ThrowHelper.MissingBatchResult(operation.Id),
                        variablesByIndex[i],
                        operation.ResultSelectionSet);
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
        ImmutableArray<VariableValues>[] variablesByIndex)
    {
        var operationCount = 0;

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

            var variables = CreateVariableValueSets(context, operation);

            if (variables.Length == 0
                && (operation.Requirements.Length > 0
                    || operation.ForwardedVariables.Length > 0))
            {
                context.TrackSkippedDefinition(this, operation);
                continue;
            }

            context.TrackVariableValueSets(this, variables);

            requestBuilder.Add(new SourceSchemaClientRequest
            {
                Node = this,
                SchemaName = schemaName,
                OperationType = operation.Operation.Type,
                OperationSourceText = operation.Operation.SourceText,
                Variables = variables,
                RequiresFileUpload = operation.RequiresFileUpload
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
}
