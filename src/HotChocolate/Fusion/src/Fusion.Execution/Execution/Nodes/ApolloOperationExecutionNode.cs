using System.Buffers;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.ApolloFederation;
using HotChocolate.Fusion.Execution.Clients;

namespace HotChocolate.Fusion.Execution.Nodes;

internal sealed class ApolloOperationExecutionNode : ExecutionNode
{
    private static readonly SelectionPath s_entitiesSource =
      SelectionPath.Root.AppendField("_entities");

    private readonly OperationRequirement[] _requirements;
    private readonly string[] _forwardedVariables;
    private readonly ResultSelectionSet _resultSelectionSet;
    private readonly ExecutionNodeCondition[] _conditions;
    private readonly bool _requiresFileUpload;
    private readonly OperationSourceText _operation;
    private readonly ulong _operationHash;
    private readonly string? _schemaName;
    private readonly SelectionPath _target;
    private readonly SelectionPath _source;

    private ApolloOperationExecutionNode(
        int id,
        OperationSourceText operation,
        string? schemaName,
        SelectionPath target,
        SelectionPath source,
        OperationRequirement[] requirements,
        string[] forwardedVariables,
        ResultSelectionSet resultSelectionSet,
        ExecutionNodeCondition[] conditions,
        bool requiresFileUpload)
    {
        Id = id;
        _operation = operation;
        _operationHash = operation.SourceText.ComputeHash();
        _schemaName = schemaName;
        _target = target;
        _source = source;
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
        EntityOperationRewriter rewriter)
    {
        operation = rewriter.Rewrite(schemaName, operation);

        return new ApolloOperationExecutionNode(
            id,
            operation,
            schemaName,
            target,
            s_entitiesSource,
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
    public SelectionPath Source => _source;

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
        var variables = context.CreateVariableValueSets(_target, _forwardedVariables, _requirements);

        if (variables.Length == 0 && (_requirements.Length > 0 || _forwardedVariables.Length > 0))
        {
            return ExecutionStatus.Skipped;
        }

        var schemaName = _schemaName ?? context.GetDynamicSchemaName(this);
        context.TrackVariableValueSets(this, variables);

        var request = new SourceSchemaClientRequest
        {
            Node = this,
            SchemaName = schemaName,
            OperationType = _operation.Type,
            OperationSourceText = _operation.SourceText,
            Variables = variables,
            RequiresFileUpload = _requiresFileUpload,
            OperationHash = _operationHash
        };

        var index = 0;
        var bufferLength = 0;
        SourceSchemaResult[]? buffer = null;
        SourceSchemaResult? singleResult = null;
        var hasSomeErrors = false;

        try
        {
            // we execute the GraphQL request against a source schema
            var client = context.GetClient(schemaName, _operation.Type);
            using var clientScope = diagnosticEvents.ExecuteSourceSchemaRequest(context, this, schemaName);

            // we read the responses from the response stream.
            var initialBufferLength = Math.Max(variables.Length, 2);

            await foreach (var result in client.ExecuteAsync(context, request, cancellationToken).ConfigureAwait(false))
            {
                // If there is only one response, we skip the buffer rental.
                if (index == 0)
                {
                    singleResult = result;
                    index = 1;
                }
                else
                {
                    // If we have more than one response, we rent a buffer and move the first result into it.
                    if (buffer is null)
                    {
                        bufferLength = initialBufferLength;
                        buffer = ArrayPool<SourceSchemaResult>.Shared.Rent(bufferLength);
                        buffer[0] = singleResult!;
                    }

                    buffer[index++] = result;
                }

                // Parsing errors here allows the result store to reuse the cached value
                // and avoids a second document lookup per result.
                if (result.Errors is not null)
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

            // if there is an error, we need to make sure that the pooled buffers for the JsonDocuments
            // are returned to the pool.
            if (buffer is not null && bufferLength > 0)
            {
                foreach (var result in buffer.AsSpan(0, index))
                {
                    // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                    result?.Dispose();
                }

                buffer.AsSpan(0, index).Clear();
                ArrayPool<SourceSchemaResult>.Shared.Return(buffer);
            }
            else
            {
                singleResult?.Dispose();
            }

            context.AddErrors(exception, variables, _resultSelectionSet);
            return ExecutionStatus.Failed;
        }

        var pendingMerge = default(PendingMerge);
        var hasPendingMerge = false;

        try
        {
            if (buffer is not null)
            {
                pendingMerge = PendingMerge.Multiple(
                    this,
                    schemaName,
                    _source,
                    _resultSelectionSet,
                    variables,
                    buffer,
                    index,
                    hasSomeErrors);
                hasPendingMerge = true;
            }
            else if (singleResult is not null)
            {
                pendingMerge = PendingMerge.Single(
                    this,
                    schemaName,
                    _source,
                    _resultSelectionSet,
                    variables,
                    singleResult,
                    hasSomeErrors);
                hasPendingMerge = true;
            }

            if (hasPendingMerge)
            {
                context.EnqueuePendingMerge(pendingMerge);
            }

            buffer = null;
            singleResult = null;
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
            else if (buffer is not null)
            {
                foreach (var result in buffer.AsSpan(0, index))
                {
                    result?.Dispose();
                }

                buffer.AsSpan(0, index).Clear();
                ArrayPool<SourceSchemaResult>.Shared.Return(buffer);
            }
            else
            {
                singleResult?.Dispose();
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
        return context.DiagnosticEvents.ExecuteOperationNode(context, this, schemaName);
    }
}
