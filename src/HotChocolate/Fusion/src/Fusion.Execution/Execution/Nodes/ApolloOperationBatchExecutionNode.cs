using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

internal sealed class ApolloOperationBatchExecutionNode : OperationBatchExecutionNode
{
    private readonly OperationDefinition[] _operations;
    private readonly ApolloEntityOperation[] _entityOperations;
    private readonly OperationSourceText _combinedOperation;
    private readonly ulong _combinedOperationHash;

    internal ApolloOperationBatchExecutionNode(
        int id,
        OperationDefinition[] operations,
        ApolloEntityOperation[] entityOperations)
        : base(id, operations)
    {
        ArgumentNullException.ThrowIfNull(entityOperations);

        if (operations.Length != entityOperations.Length)
        {
            throw new ArgumentException(
                "The Apollo entity operation count must match the operation definition count.",
                nameof(entityOperations));
        }

        _operations = operations;
        _entityOperations = entityOperations;
        _combinedOperation = CreateCombinedOperation(entityOperations);
        _combinedOperationHash = _combinedOperation.SourceText.ComputeHash();
    }

    internal ReadOnlySpan<ApolloEntityOperation> EntityOperations => _entityOperations;

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
        var entityOperation = _entityOperations[0];
        var variables = CreateVariableValueSets(context, operation);

        if (variables.Length == 0
            && (operation.Requirements.Length > 0
                || operation.ForwardedVariables.Length > 0))
        {
            return ExecutionStatus.Skipped;
        }

        context.TrackVariableValueSets(this, variables);

        var apolloVariables = ApolloEntityExecution.CreateVariables(
            variables,
            entityOperation,
            out var variablesBuffer);

        var request = new SourceSchemaClientRequest
        {
            Node = this,
            SchemaName = schemaName,
            OperationType = entityOperation.Operation.Type,
            OperationSourceText = entityOperation.Operation.SourceText,
            Variables = [apolloVariables],
            RequiresFileUpload = false,
            OperationHash = entityOperation.Operation.SourceText.ComputeHash()
        };

        var results = new List<SourceSchemaResult>(Math.Max(variables.Length, 1));
        var hasSomeErrors = false;

        try
        {
            var client = context.GetClient(schemaName, entityOperation.Operation.Type);
            await foreach (var rawResult in client.ExecuteAsync(context, request, cancellationToken).ConfigureAwait(false))
            {
                ApolloEntityExecution.SplitEntities(
                    rawResult,
                    variables,
                    results);
            }

            if (results.Count > 0)
            {
                var span = CollectionsMarshal.AsSpan(results);

                for (var i = 0; i < span.Length; i++)
                {
                    if (span[i].Errors is not null)
                    {
                        hasSomeErrors = true;
                    }
                }

                context.AddPartialResults(
                    SelectionPath.Root,
                    span,
                    operation.ResultSelectionSet,
                    hasSomeErrors);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            DisposeResults(results);
            return ExecutionStatus.Failed;
        }
        catch (Exception exception)
        {
            DisposeResults(results);
            diagnosticEvents.SourceSchemaTransportError(context, this, schemaName, exception);
            context.AddErrors(exception, variables, operation.ResultSelectionSet);
            return ExecutionStatus.Failed;
        }
        finally
        {
            variablesBuffer.Dispose();
        }

        return hasSomeErrors ? ExecutionStatus.PartialSuccess : ExecutionStatus.Success;
    }

    private async ValueTask<ExecutionStatus> ExecuteBatchAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken)
    {
        var diagnosticEvents = context.DiagnosticEvents;
        var schemaName = SchemaName;
        var activeOperations = new bool[_operations.Length];
        var variableSetsByOperation = new ImmutableArray<VariableValues>[_operations.Length];
        var activeCount = 0;
        var firstActiveOperation = -1;

        for (var i = 0; i < _operations.Length; i++)
        {
            var operation = _operations[i];

            if (IsSkipped(context, operation) || HasSkippedDependencies(context, operation))
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

            activeOperations[i] = true;
            firstActiveOperation = firstActiveOperation == -1 ? i : firstActiveOperation;
            activeCount++;
            variableSetsByOperation[i] = variables;
            context.TrackVariableValueSets(this, variables);
        }

        if (activeCount == 0)
        {
            return ExecutionStatus.Skipped;
        }

        var apolloVariables = ApolloEntityExecution.CreateBatchVariables(
            variableSetsByOperation,
            activeOperations,
            _entityOperations,
            out var variablesBuffer);

        var request = new SourceSchemaClientRequest
        {
            Node = this,
            SchemaName = schemaName,
            OperationType = _combinedOperation.Type,
            OperationSourceText = _combinedOperation.SourceText,
            Variables = [apolloVariables],
            RequiresFileUpload = false,
            OperationHash = _combinedOperationHash
        };

        var overallStatus = ExecutionStatus.Success;

        try
        {
            var client = context.GetClient(schemaName, _combinedOperation.Type);
            await foreach (var rawResult in client.ExecuteAsync(context, request, cancellationToken).ConfigureAwait(false))
            {
                if (rawResult.Data.ValueKind != JsonValueKind.Object)
                {
                    var operation = _operations[firstActiveOperation];
                    var variables = variableSetsByOperation[firstActiveOperation];
                    var result = rawResult.WithOwnedPath(
                        variables.IsDefaultOrEmpty ? CompactPath.Root : variables[0].Path,
                        variables.IsDefaultOrEmpty ? default : variables[0].AdditionalPaths);
                    var hasErrors = result.Errors is not null;

                    context.AddPartialResult(
                        operation.Source,
                        result,
                        operation.ResultSelectionSet,
                        hasErrors);

                    if (hasErrors && overallStatus == ExecutionStatus.Success)
                    {
                        overallStatus = ExecutionStatus.PartialSuccess;
                    }

                    continue;
                }

                var data = rawResult.Data;
                var sourceDocumentOwnerAssigned = false;

                for (var i = 0; i < _operations.Length; i++)
                {
                    if (!activeOperations[i])
                    {
                        continue;
                    }

                    var operation = _operations[i];
                    var entityOperation = _entityOperations[i];
                    var results = new List<SourceSchemaResult>(
                        Math.Max(variableSetsByOperation[i].Length, 1));

                    ApolloEntityExecution.SplitAliasedEntities(
                        context.Memory,
                        rawResult,
                        data,
                        variableSetsByOperation[i],
                        "____request" + i,
                        results,
                        ref sourceDocumentOwnerAssigned);

                    if (results.Count == 0)
                    {
                        continue;
                    }

                    var span = CollectionsMarshal.AsSpan(results);

                    try
                    {
                        context.AddPartialResults(
                            SelectionPath.Root,
                            span,
                            operation.ResultSelectionSet,
                            containsErrors: false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        DisposeResults(results);
                        return ExecutionStatus.Failed;
                    }
                    catch (Exception exception)
                    {
                        DisposeResults(results);
                        diagnosticEvents.SourceSchemaStoreError(context, this, schemaName, exception);
                        context.AddErrors(
                            exception,
                            variableSetsByOperation[i],
                            operation.ResultSelectionSet);
                        overallStatus = ExecutionStatus.Failed;
                    }
                }

                if (!sourceDocumentOwnerAssigned)
                {
                    rawResult.Dispose();
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

            for (var i = 0; i < _operations.Length; i++)
            {
                if (activeOperations[i])
                {
                    context.AddErrors(
                        exception,
                        variableSetsByOperation[i],
                        _operations[i].ResultSelectionSet);
                }
            }

            return ExecutionStatus.Failed;
        }
        finally
        {
            variablesBuffer.Dispose();
        }

        return overallStatus;
    }

    internal static OperationSourceText CreateCombinedOperation(ApolloEntityOperation[] entityOperations)
    {
        var sb = new StringBuilder();
        sb.Append("query (");

        for (var i = 0; i < entityOperations.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }

            sb.Append("$r");
            sb.Append(i);
            sb.Append(": [_Any!]!");
        }

        sb.AppendLine(") {");

        for (var i = 0; i < entityOperations.Length; i++)
        {
            sb.Append("  ____request");
            sb.Append(i);
            sb.Append(": _entities(representations: $r");
            sb.Append(i);
            sb.AppendLine(") {");

            using var reader = new StringReader(entityOperations[i].InlineFragmentSourceText);
            string? line;

            while ((line = reader.ReadLine()) is not null)
            {
                sb.Append("    ");
                sb.AppendLine(line);
            }

            sb.AppendLine("  }");
        }

        sb.Append('}');

        return OperationSourceText.Create(string.Empty, OperationType.Query, sb.ToString());
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

    private static void DisposeResults(List<SourceSchemaResult> results)
    {
        foreach (var result in results)
        {
            result.Dispose();
        }
    }
}
