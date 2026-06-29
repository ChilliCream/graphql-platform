using System.Runtime.InteropServices;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Clients;

namespace HotChocolate.Fusion.Execution.Nodes;

internal sealed class ApolloOperationExecutionNode : OperationExecutionNode
{
    private readonly ApolloEntityOperation _entityOperation;
    private readonly ulong _operationHash;

    internal ApolloOperationExecutionNode(
        OperationExecutionNode node,
        ApolloEntityOperation entityOperation)
        : base(
            node.Id,
            entityOperation.Operation,
            node.SchemaName,
            node.Target,
            node.Source,
            node.Requirements.ToArray(),
            node.ForwardedVariables.ToArray(),
            node.ResultSelectionSet,
            node.Conditions.ToArray(),
            node.RequiresFileUpload)
    {
        _entityOperation = entityOperation;
        _operationHash = entityOperation.Operation.SourceText.ComputeHash();

        foreach (var parentDependency in node.BufferedParentDependencies)
        {
            AddParentDependency(parentDependency);
        }
    }

    internal ApolloOperationExecutionNode(
        int id,
        string? schemaName,
        SelectionPath target,
        SelectionPath source,
        OperationRequirement[] requirements,
        string[] forwardedVariables,
        ResultSelectionSet resultSelectionSet,
        ExecutionNodeCondition[] conditions,
        bool requiresFileUpload,
        ApolloEntityOperation entityOperation)
        : base(
            id,
            entityOperation.Operation,
            schemaName,
            target,
            source,
            requirements,
            forwardedVariables,
            resultSelectionSet,
            conditions,
            requiresFileUpload)
    {
        _entityOperation = entityOperation;
        _operationHash = entityOperation.Operation.SourceText.ComputeHash();
    }

    internal ApolloEntityOperation EntityOperation => _entityOperation;

    protected override async ValueTask<ExecutionStatus> OnExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
    {
        var diagnosticEvents = context.DiagnosticEvents;
        var variables = context.CreateVariableValueSets(Target, ForwardedVariables, Requirements);

        if (variables.Length == 0 && (Requirements.Length > 0 || ForwardedVariables.Length > 0))
        {
            return ExecutionStatus.Skipped;
        }

        var schemaName = SchemaName ?? context.GetDynamicSchemaName(this);
        context.TrackVariableValueSets(this, variables);

        var apolloVariables = ApolloEntityExecution.CreateVariables(
            variables,
            _entityOperation,
            out var variablesBuffer);

        var request = new SourceSchemaClientRequest
        {
            Node = this,
            SchemaName = schemaName,
            OperationType = _entityOperation.Operation.Type,
            OperationSourceText = _entityOperation.Operation.SourceText,
            Variables = [apolloVariables],
            RequiresFileUpload = false,
            OperationHash = _operationHash
        };

        var results = new List<SourceSchemaResult>(Math.Max(variables.Length, 1));
        var hasSomeErrors = false;

        try
        {
            var client = context.GetClient(schemaName, _entityOperation.Operation.Type);
            using var clientScope = diagnosticEvents.ExecuteSourceSchemaRequest(context, this, schemaName);
            await foreach (var rawResult in client.ExecuteAsync(context, request, cancellationToken).ConfigureAwait(false))
            {
                ApolloEntityExecution.SplitEntities(
                    rawResult,
                    variables,
                    results);
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
            context.AddErrors(exception, variables, ResultSelectionSet);
            return ExecutionStatus.Failed;
        }
        finally
        {
            variablesBuffer.Dispose();
        }

        try
        {
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
                    ResultSelectionSet,
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
            context.AddErrors(exception, variables, ResultSelectionSet);
            return ExecutionStatus.Failed;
        }

        return hasSomeErrors ? ExecutionStatus.PartialSuccess : ExecutionStatus.Success;
    }

    private static void DisposeResults(List<SourceSchemaResult> results)
    {
        foreach (var result in results)
        {
            result.Dispose();
        }
    }
}
