using System.Buffers;
using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.ApolloFederation;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Executes multiple Apollo Federation entity lookups against the same source
/// schema. Each lookup is sent as its own <c>_entities</c> request with its own
/// representations variable, and the source schema client transports the
/// requests as a batch.
/// </summary>
/// <remarks>
/// Per-definition skipped-dependency drops require the plan builder to wire
/// each operation definition's dependencies. Definitions without wired
/// dependencies are never dropped for skipped dependencies.
/// </remarks>
public sealed class ApolloOperationBatchExecutionNode : ExecutionNode
{
    private static readonly SelectionPath s_entitiesSource =
        SelectionPath.Root.AppendField("_entities");

    private readonly SingleOperationDefinition[] _operations;
    private readonly ApolloEntityLookup[] _lookups;
    private readonly string _schemaName;

    private ApolloOperationBatchExecutionNode(
        int id,
        SingleOperationDefinition[] operations,
        ApolloEntityLookup[] lookups,
        FusionSchemaDefinition schema,
        string schemaName)
    {
        Id = id;
        _operations = operations;
        _lookups = new ApolloEntityLookup[lookups.Length];
        _schemaName = schemaName;

        for (var i = 0; i < lookups.Length; i++)
        {
            var lookup = lookups[i];

            if (!lookup.RepresentationShape.IsDefault)
            {
                throw new ArgumentException(
                    "An Apollo entity lookup must be unmaterialized before node construction.",
                    nameof(lookups));
            }

            var representationShape = RepresentationShapeBuilder.Build(
                lookup.Operation,
                lookup.EntityTypeName,
                operations[i].Target,
                operations[i].Requirements,
                operations[i].ResultSelectionSet,
                schema);

            _lookups[i] = lookup with { RepresentationShape = representationShape };
        }
    }

    internal static ApolloOperationBatchExecutionNode CreateFromLookup(
        int id,
        SingleOperationDefinition[] operations,
        FusionSchemaDefinition schema)
    {
        ArgumentNullException.ThrowIfNull(operations);
        ArgumentNullException.ThrowIfNull(schema);

        var schemaName = ValidateOperations(operations);
        var lookups = new ApolloEntityLookup[operations.Length];

        for (var i = 0; i < operations.Length; i++)
        {
            lookups[i] = RewriteLookup(operations[i].SourceText, schemaName, schema);
        }

        return new ApolloOperationBatchExecutionNode(
            id,
            operations,
            lookups,
            schema,
            schemaName);
    }

    internal static ApolloOperationBatchExecutionNode CreateFromParser(
        int id,
        SingleOperationDefinition[] operations,
        ApolloEntityLookup[] lookups,
        FusionSchemaDefinition schema)
    {
        ArgumentNullException.ThrowIfNull(operations);
        ArgumentNullException.ThrowIfNull(lookups);
        ArgumentNullException.ThrowIfNull(schema);

        if (operations.Length != lookups.Length)
        {
            throw new ArgumentException(
                "An Apollo entity batch requires one parsed entity lookup per "
                + "operation definition.",
                nameof(lookups));
        }

        var schemaName = ValidateOperations(operations);

        return new ApolloOperationBatchExecutionNode(
            id,
            operations,
            lookups,
            schema,
            schemaName);
    }

    /// <inheritdoc />
    public override int Id { get; }

    /// <inheritdoc />
    public override ExecutionNodeType Type => ExecutionNodeType.OperationBatch;

    /// <inheritdoc />
    public override ReadOnlySpan<ExecutionNodeCondition> Conditions => [];

    /// <inheritdoc />
    public override string SchemaName => _schemaName;

    internal ReadOnlySpan<SingleOperationDefinition> Operations => _operations;

    internal ReadOnlySpan<ApolloEntityLookup> Lookups => _lookups;

    protected override IDisposable CreateScope(OperationPlanContext context)
        => context.DiagnosticEvents.ExecuteApolloOperationBatchExecutionNode(context, this, _schemaName);

    protected override async ValueTask<ExecutionStatus> OnExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
    {
        var diagnosticEvents = context.DiagnosticEvents;
        var schemaName = _schemaName;

        var requestBuilder = ImmutableArray.CreateBuilder<SourceSchemaClientRequest>(_operations.Length);
        var operationByIndex = ArrayPool<SingleOperationDefinition>.Shared.Rent(_operations.Length);
        var representationByIndex = ArrayPool<RepresentationValue>.Shared.Rent(_operations.Length);
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
                representationByIndex,
                variablesByIndex);

            if (operationCount == 0)
            {
                return ExecutionStatus.Skipped;
            }

            var requests = requestBuilder.DrainToImmutable();

            // Obtain a transport client for the source schema and stream the batch
            // response. As each individual result arrives, we queue its merge for
            // the executor loop so downstream nodes can consume the data after completion.
            var client = context.GetClient(schemaName, requests[0].OperationType);
            receivedResults.AsSpan(0, operationCount).Clear();
            var overallStatus = ExecutionStatus.Success;

            using var clientScope = diagnosticEvents.ExecuteSourceSchemaRequest(context, this, schemaName);

            await foreach (var batchResult in client.ExecuteBatchAsync(context, requests, cancellationToken)
                .ConfigureAwait(false))
            {
                var requestIndex = batchResult.RequestIndex;
                var op = operationByIndex[requestIndex];
                var result = batchResult.Result;
                var hasErrors = result.Errors is not null;

                receivedResults[requestIndex] = true;

                var pendingMerge = default(PendingMerge);
                var hasPendingMerge = false;

                try
                {
                    pendingMerge = PendingMerge.RepresentationSingle(
                        this,
                        schemaName,
                        s_entitiesSource,
                        op.ResultSelectionSet,
                        variablesByIndex[requestIndex],
                        representationByIndex[requestIndex],
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

                    return ExecutionStatus.Failed;
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
            representationByIndex.AsSpan(0, operationCount).Clear();
            variablesByIndex.AsSpan(0, operationCount).Clear();
            ArrayPool<SingleOperationDefinition>.Shared.Return(operationByIndex);
            ArrayPool<RepresentationValue>.Shared.Return(representationByIndex);
            ArrayPool<ImmutableArray<VariableValues>>.Shared.Return(variablesByIndex);
            ArrayPool<bool>.Shared.Return(receivedResults);
        }
    }

    private int BuildRequests(
        OperationPlanContext context,
        string schemaName,
        ImmutableArray<SourceSchemaClientRequest>.Builder requestBuilder,
        SingleOperationDefinition[] operationByIndex,
        RepresentationValue[] representationByIndex,
        ImmutableArray<VariableValues>[] variablesByIndex)
    {
        var operationCount = 0;

        for (var i = 0; i < _operations.Length; i++)
        {
            var operation = _operations[i];
            var lookup = _lookups[i];

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

            var representation = context.CreateRepresentationVariableValue(
                operation.Target,
                operation.ForwardedVariables,
                operation.Requirements,
                lookup.RepresentationShape,
                lookup.EntityTypeName);

            if (representation.IsEmpty)
            {
                context.TrackSkippedDefinition(this, operation);
                continue;
            }

            // The combined representations object must be the request's single
            // variable set so the request is routed as a single operation. The
            // per-entity variable value sets are only used for error attribution.
            var requestVariables = ImmutableArray.Create(
                new VariableValues(CompactPath.Root, representation.Value));
            context.TrackVariableValueSets(this, requestVariables);

            requestBuilder.Add(new SourceSchemaClientRequest
            {
                Node = this,
                SchemaName = schemaName,
                OperationType = lookup.Operation.Type,
                OperationSourceText = lookup.Operation,
                OperationDocument = lookup.OperationDocument,
                LookupTypeName = lookup.EntityTypeName,
                Variables = requestVariables,
                RequiresFileUpload = false
            });

            operationByIndex[operationCount] = operation;
            representationByIndex[operationCount] = representation;
            variablesByIndex[operationCount] = representation.ToVariableValues();
            operationCount++;
        }

        return operationCount;
    }

    protected override bool IsSkipped(OperationPlanContext context)
    {
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
        if (context.IsNodeSkipped(operation.Id))
        {
            return true;
        }

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

    protected override void OnSealingNode()
    {
        foreach (var operation in _operations)
        {
            operation.Seal();
        }
    }

    private static ApolloEntityLookup RewriteLookup(
        OperationSourceText lookupOperation,
        string schemaName,
        FusionSchemaDefinition schema)
    {
        var rewritten = LookupEntityQueryRewriter.Rewrite(schema, schemaName, lookupOperation);

        return new ApolloEntityLookup(
            rewritten.SourceText,
            Utf8GraphQLOperationParser.Parse(rewritten.SourceText.Value, ParserOptions.Trusted),
            rewritten.EntityTypeName,
            RepresentationShape: default);
    }

    private static string ValidateOperations(SingleOperationDefinition[] operations)
    {
        if (operations.Length < 2)
        {
            throw new ArgumentException(
                "An Apollo entity batch requires at least two operation definitions.",
                nameof(operations));
        }

        var schemaName = operations[0].SchemaName;

        if (string.IsNullOrEmpty(schemaName))
        {
            throw new ArgumentException(
                "An Apollo entity batch requires a statically known source schema name.",
                nameof(operations));
        }

        foreach (var definition in operations)
        {
            if (!string.Equals(definition.SchemaName, schemaName, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "All operation definitions of an Apollo entity batch must target "
                    + "the same source schema.",
                    nameof(operations));
            }
        }

        return schemaName;
    }
}
