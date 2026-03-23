using System.Collections.Immutable;
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

    protected override async ValueTask<ExecutionStatus> OnExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
    {
        var diagnosticEvents = context.DiagnosticEvents;
        var schemaName = SchemaName;

        // Build the list of requests that will be sent as a single batch to the
        // downstream source schema. Each operation definition becomes one request
        // in the batch, and we track which operation sits at which index so we can
        // match results back to operations when the responses stream in.
        var requestBuilder = ImmutableArray.CreateBuilder<SourceSchemaClientRequest>(_operations.Length);
        var operationByIndex = new List<OperationDefinition>(_operations.Length);
        var variablesByIndex = new List<ImmutableArray<VariableValues>>(_operations.Length);

        if (_operations.Length == 1 && _operations[0] is SingleOperationDefinition)
        {
            // When the batch holds a single non-merged operation, the planner
            // promotes all of its dependencies onto the batch node as required.
            // So if we reach this point, every dependency has already succeeded.
            // We can skip the per-operation condition and dependency checks
            // entirely, which avoids unnecessary work for the common case.
            // Note: BatchOperationDefinition (merged multi-target ops) uses the
            // slow path because its deps are optional: some targets' deps may
            // be skipped while others succeed.
            var operation = _operations[0];

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

            if (variables.Length == 0 && (operation.Requirements.Length > 0 || operation.ForwardedVariables.Length > 0))
            {
                return ExecutionStatus.Skipped;
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

            operationByIndex.Add(operation);
            variablesByIndex.Add(variables);
        }
        else
        {
            foreach (var operation in _operations)
            {
                if (IsSkipped(context, operation))
                {
                    context.TrackSkippedDefinition(this, operation);
                    continue;
                }

                // If any of this operation's dependencies were skipped or failed,
                // we skip this operation within the batch. The remaining operations
                // whose dependencies succeeded can still proceed normally.
                if (HasSkippedDependencies(context, operation))
                {
                    context.TrackSkippedDefinition(this, operation);
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

                // The operation expects input (requirements or forwarded variables), but
                // the result store produced no matching variable values. Without input
                // there is nothing meaningful to fetch, so we skip this operation.
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

                operationByIndex.Add(operation);
                variablesByIndex.Add(variables);
            }
        }

        // Every operation in the batch was either skipped or had no variable
        // values to resolve. There is nothing to send to the downstream service.
        if (requestBuilder.Count == 0)
        {
            return ExecutionStatus.Skipped;
        }

        var requests = requestBuilder.ToImmutable();

        // Obtain a transport client for the source schema and stream the batch
        // response. As each individual result arrives, we merge it into the
        // result store so downstream nodes can consume the data.
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
                    context.AddErrors(exception, variablesByIndex[requestIndex], op.ResultSelectionSet);
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

            // The transport itself failed, so every operation in the batch is affected.
            // We attach the error to each operation's result selection set.
            for (var i = 0; i < operationByIndex.Count; i++)
            {
                context.AddErrors(exception, variablesByIndex[i], operationByIndex[i].ResultSelectionSet);
            }

            return ExecutionStatus.Failed;
        }

        // Verify that the downstream service returned a result for every
        // operation in the batch. A missing result means the service did
        // not implement the batch protocol correctly. We surface this as
        // an error so the issue is easy to diagnose.
        var missingCount = 0;

        for (var i = 0; i < receivedResults.Length; i++)
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
            overallStatus = missingCount == receivedResults.Length
                ? ExecutionStatus.Failed
                : ExecutionStatus.PartialSuccess;
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

    protected override void OnSealingNode()
    {
        foreach (var operation in _operations)
        {
            operation.Seal();
        }
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
}
