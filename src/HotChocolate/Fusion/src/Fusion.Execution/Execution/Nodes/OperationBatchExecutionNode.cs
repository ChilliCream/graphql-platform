using System.Buffers;
using System.Collections.Immutable;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Text.Json;
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
        SchemaName = operations[0].SchemaName;
    }

    public override int Id { get; }

    public override ExecutionNodeType Type => ExecutionNodeType.OperationBatch;

    public override ReadOnlySpan<ExecutionNodeCondition> Conditions => [];

    public override string? SchemaName { get; }

    internal ReadOnlySpan<OperationDefinition> Operations => _operations;

    protected override IDisposable? CreateScope(OperationPlanContext context)
    {
        var schemaName = SchemaName ?? context.GetDynamicSchemaName(this);
        return context.DiagnosticEvents.ExecuteOperationBatchNode(context, this, schemaName);
    }

    protected override async ValueTask<ExecutionStatus> OnExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
    {
        var diagnosticEvents = context.DiagnosticEvents;
        var schemaName = SchemaName ?? context.GetDynamicSchemaName(this);

        // First we will be merging all operations into a single batch requests.
        var requestBuilder = ImmutableArray.CreateBuilder<SourceSchemaClientRequest>(_operations.Length);
        var operationByIndex = new List<OperationDefinition>(_operations.Length);
        var variablesByIndex = new List<ImmutableArray<VariableValues>>(_operations.Length);

        foreach (var operation in _operations)
        {
            if (IsSkipped(context, operation))
            {
                continue;
            }

            // If any of this operation's dependencies were skipped or failed,
            // skip this operation within the batch but let the batch continue
            // for other operations whose dependencies succeeded.
            if (HasSkippedDependencies(context, operation))
            {
                continue;
            }

            var variables = operation switch
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

            // if the operation has requirements or forwarded variables but we could not
            // resolve any variable values from the result store, there is nothing to fetch
            // so we just skip this operation.
            if (variables.Length == 0 && (operation.Requirements.Length > 0 || operation.ForwardedVariables.Length > 0))
            {
                continue;
            }

            context.TrackVariableValueSets(this, variables);

            requestBuilder.Add(new SourceSchemaClientRequest
            {
                Node = this,
                SchemaName = schemaName,
                BatchingGroupId = operation.BatchingGroupId,
                OperationType = operation.Operation.Type,
                OperationSourceText = operation.Operation.SourceText,
                Variables = variables,
                RequiresFileUpload = operation.RequiresFileUpload
            });

            operationByIndex.Add(operation);
            variablesByIndex.Add(variables);
        }

        // all operations were either skipped or had no variables to resolve,
        // so there is nothing to fetch.
        if (requestBuilder.Count == 0)
        {
            return ExecutionStatus.Skipped;
        }

        var requests = requestBuilder.ToImmutable();

        // next we will get a client for the source schema and execute the batch request and update the result store.
        var client = context.GetClient(schemaName, requests[0].OperationType);
        var receivedResults = new bool[requests.Length];
        var overallStatus = ExecutionStatus.Success;

        try
        {
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
                    AddErrors(context, exception, variablesByIndex[requestIndex], op.ResultSelectionSet);
                    overallStatus = ExecutionStatus.Failed;
                    continue;
                }

                if (hasErrors && overallStatus == ExecutionStatus.Success)
                {
                    overallStatus = ExecutionStatus.PartialSuccess;
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

            // Transport error: add errors for all operations.
            for (var i = 0; i < operationByIndex.Count; i++)
            {
                AddErrors(context, exception, variablesByIndex[i], operationByIndex[i].ResultSelectionSet);
            }

            return ExecutionStatus.Failed;
        }

        // Phase 3 — Handle missing results: any request index that never got results.
        for (var i = 0; i < receivedResults.Length; i++)
        {
            if (!receivedResults[i])
            {
                var missingOp = operationByIndex[i];
                var missingException = new InvalidOperationException(
                    $"The batch response does not contain any result for operation '{missingOp.Id}'.");
                AddErrors(context, missingException, variablesByIndex[i], missingOp.ResultSelectionSet);
                overallStatus = ExecutionStatus.Failed;
            }
        }

        return overallStatus;
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
                throw new InvalidOperationException(
                    $"Expected to have a boolean value for variable '${condition.VariableName}'");
            }

            if (booleanValueNode.Value != condition.PassingValue)
            {
                return true;
            }
        }

        return false;
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

    private static void AddErrors(
        OperationPlanContext context,
        Exception exception,
        ImmutableArray<VariableValues> variables,
        ResultSelectionSet resultSelectionSet)
    {
        var error = ErrorBuilder.FromException(exception).Build();

        if (variables.Length == 0)
        {
            context.AddErrors(error, resultSelectionSet, Path.Root);
        }
        else
        {
            var pathBufferLength = 0;

            for (var i = 0; i < variables.Length; i++)
            {
                pathBufferLength += 1 + variables[i].AdditionalPaths.Length;
            }

            var pathBuffer = ArrayPool<CompactPath>.Shared.Rent(pathBufferLength);

            try
            {
                var pathBufferIndex = 0;

                for (var i = 0; i < variables.Length; i++)
                {
                    pathBuffer[pathBufferIndex++] = variables[i].Path;

                    foreach (var additionalPath in variables[i].AdditionalPaths)
                    {
                        pathBuffer[pathBufferIndex++] = additionalPath;
                    }
                }

                context.AddErrors(error, resultSelectionSet, pathBuffer.AsSpan(0, pathBufferLength));
            }
            finally
            {
                pathBuffer.AsSpan(0, pathBufferLength).Clear();
                ArrayPool<CompactPath>.Shared.Return(pathBuffer);
            }
        }
    }
}
