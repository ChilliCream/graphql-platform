using System.Collections.Immutable;
using System.Runtime.InteropServices;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.ApolloFederation;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class ApolloOperationExecutionNode : ExecutionNode
{
    private static readonly SelectionPath s_entitiesSource =
        SelectionPath.Root.AppendField("_entities");

    private readonly OperationRequirement[] _requirements;
    private readonly string[] _forwardedVariables;
    private readonly ResultSelectionSet _resultSelectionSet;
    private readonly ExecutionNodeCondition[] _conditions;
    private readonly bool _requiresFileUpload;
    private readonly OperationSourceText _operation;
    private readonly OperationSourceText _lookupOperation;
    private readonly ulong _operationHash;
    private readonly string? _schemaName;
    private readonly SelectionPath _target;
    private readonly string _entityTypeName;
    private readonly List<RepresentationShapeNode> _representationShape;

    private ApolloOperationExecutionNode(
        int id,
        OperationSourceText operation,
        OperationSourceText lookupOperation,
        string? schemaName,
        SelectionPath target,
        string entityTypeName,
        List<RepresentationShapeNode> representationShape,
        OperationRequirement[] requirements,
        string[] forwardedVariables,
        ResultSelectionSet resultSelectionSet,
        ExecutionNodeCondition[] conditions,
        bool requiresFileUpload)
    {
        Id = id;
        _operation = operation;
        _lookupOperation = lookupOperation;
        _operationHash = operation.SourceText.ComputeHash();
        _schemaName = schemaName;
        _target = target;
        _entityTypeName = entityTypeName;
        _representationShape = representationShape;
        _requirements = requirements;
        _forwardedVariables = forwardedVariables;
        _resultSelectionSet = resultSelectionSet;
        _conditions = conditions;
        _requiresFileUpload = requiresFileUpload;
    }

    internal static ApolloOperationExecutionNode Create(
        int id,
        OperationSourceText operation,
        string? schemaName,
        SelectionPath target,
        OperationRequirement[] requirements,
        string[] forwardedVariables,
        ResultSelectionSet resultSelectionSet,
        ExecutionNodeCondition[] conditions,
        bool requiresFileUpload,
        FusionSchemaDefinition schema)
    {
        var rewritten = LookupEntityQueryRewriter.Rewrite(schema, schemaName!, operation);

        // Compile the representation shape once at plan build so that unsupported
        // requirement maps and unbound requirements fail here rather than at
        // execution time. The shape is a plan-time constant, so it is retained and
        // reused for every request this node serves.
        var representationShape =
            RepresentationShapeBuilder.Build(rewritten.LookupField, requirements);

        return new ApolloOperationExecutionNode(
            id,
            rewritten.Operation,
            operation,
            schemaName,
            target,
            rewritten.EntityTypeName,
            representationShape,
            requirements,
            forwardedVariables,
            resultSelectionSet,
            conditions,
            requiresFileUpload);
    }

    /// <inheritdoc />
    public override int Id { get; }

    /// <inheritdoc />
    public override ExecutionNodeType Type => ExecutionNodeType.Operation;

    /// <inheritdoc />
    public override ReadOnlySpan<ExecutionNodeCondition> Conditions => _conditions;

    /// <summary>
    /// Gets the operation definition that this execution node represents.
    /// </summary>
    public OperationSourceText Operation => _operation;

    /// <summary>
    /// Gets the lookup operation this node was created from, before it was
    /// rewritten into an <c>_entities</c> operation.
    /// </summary>
    internal OperationSourceText LookupOperation => _lookupOperation;

    /// <summary>
    /// Gets the result selection set fulfilled by this operation.
    /// </summary>
    internal ResultSelectionSet ResultSelectionSet => _resultSelectionSet;

    /// <inheritdoc />
    public override string? SchemaName => _schemaName;

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
        var representation = context.CreateRepresentationVariableValue(
            _target,
            _forwardedVariables,
            _requirements,
            _entityTypeName,
            _representationShape);

        if (representation.IsEmpty)
        {
            return ExecutionStatus.Skipped;
        }

        var variables = representation.ToVariableValues();
        var schemaName = _schemaName ?? context.GetDynamicSchemaName(this);

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
            OperationType = _operation.Type,
            OperationSourceText = _operation.SourceText,
            Variables = requestVariables,
            RequiresFileUpload = false,
            OperationHash = _operationHash
        };

        SourceSchemaResult? result = null;
        var hasSomeErrors = false;

        try
        {
            var client = context.GetClient(schemaName, _operation.Type);
            using var clientScope = diagnosticEvents.ExecuteSourceSchemaRequest(context, this, schemaName);

            await foreach (var current in client.ExecuteAsync(context, request, cancellationToken).ConfigureAwait(false))
            {
                if (result is not null)
                {
                    current.Dispose();
                    throw new InvalidOperationException(
                        "Apollo entity fetches must produce a single source schema result.");
                }

                result = current;

                if (current.Errors is not null)
                {
                    hasSomeErrors = true;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // If the execution of the node was cancelled, either the entire request was cancelled
            // or the execution was halted. In both cases we do not want to produce any errors
            // and just exit the node as quickly as possible.
            return ExecutionStatus.Failed;
        }
        catch (Exception exception)
        {
            diagnosticEvents.SourceSchemaTransportError(context, this, schemaName, exception);
            result?.Dispose();

            context.AddErrors(exception, variables, _resultSelectionSet);
            return ExecutionStatus.Failed;
        }

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
            }

            if (hasPendingMerge)
            {
                context.EnqueuePendingMerge(pendingMerge);
            }

            result = null;
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
                result?.Dispose();
            }

            diagnosticEvents.SourceSchemaStoreError(context, this, schemaName, exception);
            context.AddErrors(exception, variables, _resultSelectionSet);
            return ExecutionStatus.Failed;
        }

        return hasSomeErrors ? ExecutionStatus.PartialSuccess : ExecutionStatus.Success;
    }

    protected override IDisposable CreateScope(OperationPlanContext context)
    {
        var schemaName = _schemaName ?? context.GetDynamicSchemaName(this);
        return context.DiagnosticEvents.ExecuteApolloOperationExecutionNode(context, this, schemaName);
    }
}
