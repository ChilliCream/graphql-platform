using System.Buffers;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Text.Json;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class OperationBatchExecutionNode : ExecutionNode
{
    private readonly OperationRequirement[] _requirements;
    private readonly string[] _forwardedVariables;
    private readonly ResultSelectionSet _resultSelectionSet;
    private readonly ExecutionNodeCondition[] _conditions;
    private readonly bool _requiresFileUpload;
    private readonly OperationSourceText _operation;
    private readonly int? _batchingGroupId;
    private readonly string? _schemaName;
    private readonly SelectionPath[] _targets;
    private readonly SelectionPath _source;

    internal OperationBatchExecutionNode(
        int id,
        OperationSourceText operation,
        string? schemaName,
        SelectionPath[] targets,
        SelectionPath source,
        OperationRequirement[] requirements,
        string[] forwardedVariables,
        ResultSelectionSet resultSelectionSet,
        ExecutionNodeCondition[] conditions,
        int? batchingGroupId,
        bool requiresFileUpload)
    {
        Id = id;
        _operation = operation;
        _batchingGroupId = batchingGroupId;
        _schemaName = schemaName;
        _targets = targets;
        _source = source;
        _requirements = requirements;
        _forwardedVariables = forwardedVariables;
        _resultSelectionSet = resultSelectionSet;
        _conditions = conditions;
        _requiresFileUpload = requiresFileUpload;
    }

    /// <inheritdoc />
    public override int Id { get; }

    /// <inheritdoc />
    public override ExecutionNodeType Type => ExecutionNodeType.OperationBatch;

    /// <inheritdoc />
    public override ReadOnlySpan<ExecutionNodeCondition> Conditions => _conditions;

    /// <summary>
    /// Gets the operation definition that this execution node represents.
    /// </summary>
    public OperationSourceText Operation => _operation;

    /// <summary>
    /// Gets the deterministic batching group identifier assigned at planning time.
    /// </summary>
    public int? BatchingGroupId => _batchingGroupId;

    /// <summary>
    /// Gets the result selection set fulfilled by this operation.
    /// </summary>
    internal ResultSelectionSet ResultSelectionSet => _resultSelectionSet;

    /// <inheritdoc />
    public override string? SchemaName => _schemaName;

    /// <summary>
    /// Gets the paths to the selection sets for which this operation fetches data.
    /// </summary>
    public ReadOnlySpan<SelectionPath> Targets => _targets;

    /// <summary>
    /// Gets the path to the local selection set (the selection set within the source schema request)
    /// to extract the data from.
    /// </summary>
    public SelectionPath Source => _source;

    /// <summary>
    /// Gets the data requirements that are needed to execute this operation.
    /// </summary>
    public ReadOnlySpan<OperationRequirement> Requirements => _requirements;

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
        var variables = context.CreateVariableValueSets(_targets, _forwardedVariables, _requirements);

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
            BatchingGroupId = _batchingGroupId,
            OperationType = _operation.Type,
            OperationSourceText = _operation.SourceText,
            Variables = variables,
            RequiresFileUpload = _requiresFileUpload
        };

        var index = 0;
        var bufferLength = 0;
        SourceSchemaResult[]? buffer = null;
        SourceSchemaResult? singleResult = null;
        var hasSomeErrors = false;

        try
        {
            // we execute the GraphQL request against a source schema
            var response = await context.SourceSchemaScheduler
                .ExecuteAsync(request, cancellationToken)
                .ConfigureAwait(false);
            context.TrackSourceSchemaClientResponse(this, response);

            // we read the responses from the response stream.
            var totalPathCount = variables.Length;

            for (var i = 0; i < variables.Length; i++)
            {
                totalPathCount += variables[i].AdditionalPaths.Length;
            }

            var initialBufferLength = Math.Max(totalPathCount, 2);

            await foreach (var result in response.ReadAsResultStreamAsync(cancellationToken))
            {
                // Store the first result without renting a buffer,
                // since it might be the only one (e.g. a request-level error).
                if (index == 0)
                {
                    singleResult = result;
                    index = 1;
                }
                else
                {
                    // Once we see a second result, we know there are multiple,
                    // so we rent a buffer and move the first result into it.
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
            else if (singleResult is not null)
            {
                singleResult.Dispose();
            }

            AddErrors(context, exception, variables, _resultSelectionSet);
            return ExecutionStatus.Failed;
        }

        try
        {
            if (buffer is not null)
            {
                context.AddPartialResults(
                    _source,
                    buffer.AsSpan(0, index),
                    _resultSelectionSet,
                    hasSomeErrors);
            }
            else if (singleResult is not null)
            {
                var firstResult = singleResult;
                context.AddPartialResults(
                    _source,
                    MemoryMarshal.CreateReadOnlySpan(ref firstResult, 1),
                    _resultSelectionSet,
                    hasSomeErrors);
            }
            else
            {
                context.AddPartialResults(
                    _source,
                    [],
                    _resultSelectionSet,
                    hasSomeErrors);
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
            diagnosticEvents.SourceSchemaStoreError(context, this, schemaName, exception);
            AddErrors(context, exception, variables, _resultSelectionSet);
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

    protected override IDisposable CreateScope(OperationPlanContext context)
    {
        var schemaName = _schemaName ?? context.GetDynamicSchemaName(this);
        return context.DiagnosticEvents.ExecuteOperationBatchNode(context, this, schemaName);
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
