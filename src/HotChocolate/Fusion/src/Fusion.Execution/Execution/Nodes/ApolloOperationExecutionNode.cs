using System.Collections.Immutable;
using System.Runtime.InteropServices;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.ApolloFederation;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class ApolloOperationExecutionNode : ExecutionNode
{
    private static readonly SelectionPath s_entitiesSource =
        SelectionPath.Root.AppendField("_entities");

    private readonly OperationRequirement[] _requirements;
    private readonly string[] _forwardedVariables;
    private readonly ResultSelectionSet _resultSelectionSet;
    private readonly ExecutionNodeCondition[] _conditions;
    private readonly ExecutionNodeCondition[][] _conditionAlternatives;
    private readonly bool _requiresFileUpload;
    private readonly ApolloEntityLookup _lookup;
    private readonly string _schemaName;
    private readonly SelectionPath _target;

    private ApolloOperationExecutionNode(
        int id,
        ApolloEntityLookup lookup,
        string schemaName,
        SelectionPath target,
        OperationRequirement[] requirements,
        string[] forwardedVariables,
        ResultSelectionSet resultSelectionSet,
        ExecutionNodeCondition[] conditions,
        ExecutionNodeCondition[][] conditionAlternatives,
        bool requiresFileUpload,
        FusionSchemaDefinition schema)
    {
        ArgumentException.ThrowIfNullOrEmpty(schemaName);

        if (!lookup.RepresentationShape.IsDefault)
        {
            throw new ArgumentException(
                "An Apollo entity lookup must be unmaterialized before node construction.",
                nameof(lookup));
        }

        Id = id;
        lookup = lookup with
        {
            RepresentationShape =
                RepresentationShapeBuilder.Build(
                    lookup.Operation,
                    lookup.EntityTypeName,
                    target,
                    requirements,
                    resultSelectionSet,
                    schema)
        };

        _lookup = lookup;
        _schemaName = schemaName;
        _target = target;
        _requirements = requirements;
        _forwardedVariables = forwardedVariables;
        _resultSelectionSet = resultSelectionSet;
        _conditions = conditions;
        _conditionAlternatives = conditionAlternatives;
        _requiresFileUpload = requiresFileUpload;
    }

    internal static ApolloOperationExecutionNode CreateFromLookup(
        int id,
        OperationSourceText lookupOperation,
        string schemaName,
        SelectionPath target,
        OperationRequirement[] requirements,
        string[] forwardedVariables,
        ResultSelectionSet resultSelectionSet,
        ExecutionNodeCondition[] conditions,
        ExecutionNodeCondition[][] conditionAlternatives,
        bool requiresFileUpload,
        FusionSchemaDefinition schema)
    {
        var rewritten = LookupEntityQueryRewriter.Rewrite(schema, schemaName, lookupOperation);
        var lookup = new ApolloEntityLookup(
            rewritten.SourceText,
            Utf8GraphQLOperationParser.Parse(rewritten.SourceText.Value, ParserOptions.Trusted),
            rewritten.EntityTypeName,
            RepresentationShape: default);

        return new ApolloOperationExecutionNode(
            id,
            lookup,
            schemaName,
            target,
            requirements,
            forwardedVariables,
            resultSelectionSet,
            conditions,
            conditionAlternatives,
            requiresFileUpload,
            schema);
    }

    internal static ApolloOperationExecutionNode CreateFromParser(
        int id,
        OperationSourceText operation,
        string entityTypeName,
        string schemaName,
        SelectionPath target,
        OperationRequirement[] requirements,
        string[] forwardedVariables,
        ResultSelectionSet resultSelectionSet,
        ExecutionNodeCondition[] conditions,
        ExecutionNodeCondition[][] conditionAlternatives,
        bool requiresFileUpload,
        FusionSchemaDefinition schema)
    {
        ArgumentException.ThrowIfNullOrEmpty(schemaName);

        return new(
            id,
            new ApolloEntityLookup(
                operation,
                Utf8GraphQLOperationParser.Parse(operation.Value, ParserOptions.Trusted),
                entityTypeName,
                RepresentationShape: default),
            schemaName,
            target,
            requirements,
            forwardedVariables,
            resultSelectionSet,
            conditions,
            conditionAlternatives,
            requiresFileUpload,
            schema);
    }

    /// <inheritdoc />
    public override int Id { get; }

    /// <inheritdoc />
    public override ExecutionNodeType Type => ExecutionNodeType.Operation;

    /// <inheritdoc />
    public override ReadOnlySpan<ExecutionNodeCondition> Conditions => _conditions;

    /// <inheritdoc />
    public override ReadOnlySpan<ExecutionNodeCondition[]> ConditionAlternatives => _conditionAlternatives;

    /// <summary>
    /// Gets the operation definition that this execution node represents.
    /// </summary>
    public OperationSourceText Operation => _lookup.Operation;

    /// <summary>
    /// Gets the entity lookup that this operation resolves.
    /// </summary>
    internal ApolloEntityLookup Lookup => _lookup;

    /// <summary>
    /// Gets the result selection set fulfilled by this operation.
    /// </summary>
    internal ResultSelectionSet ResultSelectionSet => _resultSelectionSet;

    /// <inheritdoc />
    public override string SchemaName => _schemaName;

    /// <summary>
    /// Gets the path to the selection set for which this operation fetches data.
    /// </summary>
    public SelectionPath Target => _target;

    /// <summary>
    /// Gets the path to the local selection set (the selection set within the source schema request)
    /// to extract the data from.
    /// </summary>
    public SelectionPath Source => s_entitiesSource;

    /// <summary>
    /// Gets the data requirements that are needed to execute this operation.
    /// </summary>
    public ReadOnlySpan<OperationRequirement> Requirements => _requirements;

    internal ImmutableArray<OperationRequirement> GetRequirementsArray()
        => ImmutableCollectionsMarshal.AsImmutableArray(_requirements);

    /// <summary>
    /// Gets the variables that are needed to execute this operation.
    /// </summary>
    public ReadOnlySpan<string> ForwardedVariables => _forwardedVariables;

    /// <summary>
    /// Gets whether this operation contains one or more variables
    /// that contain the Upload scalar.
    /// </summary>
    public bool RequiresFileUpload => _requiresFileUpload;

    protected override async ValueTask<ExecutionStatus> OnExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
    {
        var diagnosticEvents = context.DiagnosticEvents;
        var reusedCount = 0;
        var representation = context.GetEntityReuseCandidates(Id) is { } candidates
            ? context.CreateRepresentationVariableValue(
                [_target],
                _forwardedVariables,
                _requirements,
                _lookup.RepresentationShape,
                _lookup.EntityTypeName,
                candidates,
                out reusedCount)
            : context.CreateRepresentationVariableValue(
                _target,
                _forwardedVariables,
                _requirements,
                _lookup.RepresentationShape,
                _lookup.EntityTypeName);

        if (representation.IsEmpty)
        {
            // When every entity was taken from the result, the node completed its work.
            return reusedCount > 0 ? ExecutionStatus.Success : ExecutionStatus.Skipped;
        }

        var variables = representation.ToVariableValues();
        var schemaName = _schemaName;

        // The combined representations object must be the request's single
        // variable set so the request is routed as a single operation. The
        // per-entity variable value sets are only used for error attribution.
        var requestVariables = ImmutableArray.Create(
            new VariableValues(CompactPath.Root, representation.Value));
        context.TrackVariableValueSets(this, requestVariables);

        var request = new SourceSchemaClientRequest
        {
            Node = this,
            SchemaName = schemaName,
            OperationType = _lookup.Operation.Type,
            OperationSourceText = _lookup.Operation,
            Variables = requestVariables,
            RequiresFileUpload = false,
            OperationDocument = _lookup.OperationDocument,
            LookupTypeName = _lookup.EntityTypeName
        };

        var results = new SourceSchemaResultCollector(initialCapacity: 2);

        try
        {
            var client = context.GetClient(schemaName, _lookup.Operation.Type);
            using var clientScope = diagnosticEvents.ExecuteSourceSchemaRequest(context, this, schemaName);

            if (client is HttpSourceSchemaClient httpClient
                && HttpSourceSchemaClient.CanExecuteBuffered(_lookup.Operation.Type, requiresFileUpload: false))
            {
                await httpClient
                    .ExecuteBufferedAsync(context, request, results, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await foreach (var current in client.ExecuteAsync(context, request, cancellationToken).ConfigureAwait(false))
                {
                    // A second result ends the read, so a source that keeps its stream open is not awaited.
                    if (results.Count > 0)
                    {
                        current.Dispose();
                        throw ThrowHelper.ApolloEntityFetchProducedMultipleResults();
                    }

                    results.OnResult(0, current);
                }
            }

            if (results.Count > 1)
            {
                throw ThrowHelper.ApolloEntityFetchProducedMultipleResults();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // If the execution of the node was cancelled, either the entire request was cancelled
            // or the execution was halted. In both cases we do not want to produce any errors
            // and just exit the node as quickly as possible.
            results.DisposeResults();
            return ExecutionStatus.Failed;
        }
        catch (Exception exception)
        {
            diagnosticEvents.SourceSchemaTransportError(context, this, schemaName, exception);
            results.DisposeResults();

            context.AddErrors(exception, variables, _resultSelectionSet);
            return ExecutionStatus.Failed;
        }

        var result = results.Single;
        var hasSomeErrors = results.HasErrors;
        var pendingMerge = default(PendingMerge);
        var hasPendingMerge = false;

        try
        {
            if (result is not null)
            {
                pendingMerge = PendingMerge.RepresentationSingle(
                    this,
                    schemaName,
                    s_entitiesSource,
                    _resultSelectionSet,
                    variables,
                    representation,
                    result,
                    hasSomeErrors);
                hasPendingMerge = true;

                // ExecuteAsync reports the completion of this node right after we return,
                // and that signal wakes the executor for the merge as well.
                context.EnqueuePendingMergeBeforeCompletion(pendingMerge);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // If the execution of the node was cancelled, either the entire request was cancelled
            // or the execution was halted. In both cases we do not want to produce any errors
            // and just exit the node as quickly as possible.
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
                results.DisposeResults();
            }

            diagnosticEvents.SourceSchemaStoreError(context, this, schemaName, exception);
            context.AddErrors(exception, variables, _resultSelectionSet);
            return ExecutionStatus.Failed;
        }

        return hasSomeErrors ? ExecutionStatus.PartialSuccess : ExecutionStatus.Success;
    }

    protected override IDisposable CreateScope(OperationPlanContext context)
    {
        return context.DiagnosticEvents.ExecuteApolloOperationExecutionNode(context, this, _schemaName);
    }
}
