using System.Collections.Immutable;
using HotChocolate.Collections.Immutable;

namespace HotChocolate.Execution;

/// <summary>
/// Represents a GraphQL operation result containing data, errors, extensions, and incremental delivery information.
/// </summary>
public sealed class OperationResult : ExecutionResult
{
    private ImmutableList<IError> _errors;
    private ImmutableOrderedDictionary<string, object?> _extensions;

    /// <summary>
    /// Initializes a new instance of <see cref="OperationResult"/> with structured data and an optional formatter.
    /// </summary>
    /// <param name="data">
    /// The operation result data with its formatter and memory management.
    /// </param>
    /// <param name="errors">
    /// The GraphQL errors that occurred during execution.
    /// </param>
    /// <param name="extensions">
    /// Additional information passed along with the result.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the data structure is invalid or when neither data, errors, nor extensions are provided.
    /// </exception>
    public OperationResult(
        OperationResultData data,
        ImmutableList<IError>? errors = null,
        ImmutableOrderedDictionary<string, object?>? extensions = null)
    {
        if (data.Value is not null && data.Formatter is null)
        {
            throw new ArgumentException("The result data structure is not supported.");
        }

        if (data.IsValueNull && errors is null or { Count: 0 } && extensions is null or { Count: 0 })
        {
            throw new ArgumentException("Either data or errors must be provided or extensions must be provided.");
        }

        Data = data;
        _errors = errors ?? [];
        _extensions = extensions ?? [];

        if (data.MemoryHolder is { } memoryHolder)
        {
            RegisterForCleanup(() =>
            {
                memoryHolder.Dispose();
                return ValueTask.CompletedTask;
            });
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="OperationResult"/> with only errors.
    /// </summary>
    /// <param name="errors">
    /// The GraphQL errors that occurred during execution.
    /// </param>
    /// <param name="extensions">
    /// Additional information passed along with the result.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the errors list is empty.
    /// </exception>
    public OperationResult(ImmutableList<IError> errors, ImmutableOrderedDictionary<string, object?>? extensions = null)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (errors.Count == 0)
        {
            throw new ArgumentException("At least one error must be provided.");
        }

        _errors = errors;
        _extensions = extensions ?? [];
    }

    /// <summary>
    /// Initializes a new instance of <see cref="OperationResult"/> with only extensions.
    /// </summary>
    /// <param name="extensions">
    /// Additional information passed along with the result.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="extensions"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the extensions dictionary is empty.
    /// </exception>
    public OperationResult(ImmutableOrderedDictionary<string, object?> extensions)
    {
        ArgumentNullException.ThrowIfNull(extensions);

        if (extensions.Count == 0)
        {
            throw new ArgumentException("At least one extension must be provided.");
        }

        _errors = [];
        _extensions = extensions;
    }

    /// <summary>
    /// Gets the kind of execution result.
    /// </summary>
    public override ExecutionResultKind Kind => ExecutionResultKind.SingleResult;

    /// <summary>
    /// Gets or initializes the index of the request that corresponds to this result.
    /// Used in batched operations to correlate results with requests.
    /// </summary>
    public int? RequestIndex { get; init; }

    /// <summary>
    /// Gets or initializes the index of the variable set that corresponds to this result.
    /// Used when executing operations with multiple variable sets.
    /// </summary>
    public int? VariableIndex { get; init; }

    /// <summary>
    /// Gets or sets the path to the insertion point for incremental delivery.
    /// Informs clients how to patch subsequent delta payloads into the original payload.
    /// </summary>
    public Path? Path
    {
        get => Features.Get<IncrementalDataFeature>()?.Path;
        set
        {
            var feature = Features.Get<IncrementalDataFeature>() ?? new IncrementalDataFeature();
            feature.Path = value;
            Features.Set(feature);
        }
    }

    /// <summary>
    /// Gets the data that is being delivered in this operation result.
    /// </summary>
    public OperationResultData? Data { get; internal set; }

    /// <summary>
    /// Gets the GraphQL errors that occurred during execution.
    /// </summary>
    public ImmutableList<IError> Errors
    {
        get => _errors;
        set
        {
            if (!Data.HasValue
                && Errors is null or { Count: 0 }
                && Extensions is null or { Count: 0 }
                && Features.Get<IncrementalDataFeature>() is null)
            {
                throw new ArgumentException("Either data, errors or extensions must be provided.");
            }

            _errors = value;
        }
    }

    /// <summary>
    /// Gets or sets additional information passed along with the result.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when setting to null or empty when data and errors are also null or empty.
    /// </exception>
    public ImmutableOrderedDictionary<string, object?> Extensions
    {
        get => _extensions;
        set
        {
            if (!Data.HasValue
                && Errors is null or { Count: 0 }
                && Extensions is null or { Count: 0 }
                && Features.Get<IncrementalDataFeature>() is null)
            {
                throw new ArgumentException("Either data, errors or extensions must be provided.");
            }

            _extensions = value;
        }
    }

    /// <summary>
    /// Gets or sets the list of pending incremental delivery operations.
    /// Each pending result announces data that will be delivered incrementally in subsequent payloads.
    /// </summary>
    public ImmutableList<PendingResult> Pending
    {
        get => Features.Get<IncrementalDataFeature>()?.Pending ?? [];
        set
        {
            var feature = Features.Get<IncrementalDataFeature>() ?? new IncrementalDataFeature();
            feature.Pending = value;
            Features.Set(feature);
        }
    }

    /// <summary>
    /// Gets or sets the list of incremental results containing data from @defer or @stream directives.
    /// Contains the actual data for previously announced pending operations.
    /// </summary>
    public ImmutableList<IIncrementalResult> Incremental
    {
        get => Features.Get<IncrementalDataFeature>()?.Incremental ?? [];
        set
        {
            var feature = Features.Get<IncrementalDataFeature>() ?? new IncrementalDataFeature();
            feature.Incremental = value;
            Features.Set(feature);
        }
    }

    /// <summary>
    /// Gets or sets the list of completed incremental delivery operations.
    /// Each completed result indicates that all data for a pending operation has been delivered.
    /// </summary>
    public ImmutableList<CompletedResult> Completed
    {
        get => Features.Get<IncrementalDataFeature>()?.Completed ?? [];
        set
        {
            var feature = Features.Get<IncrementalDataFeature>() ?? new IncrementalDataFeature();
            feature.Completed = value;
            Features.Set(feature);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether more payloads will follow in the response stream.
    /// When <c>true</c>, clients should expect additional incremental data.
    /// When <c>false</c>, this is the final payload.
    /// </summary>
    public bool? HasNext
    {
        get => Features.Get<IncrementalDataFeature>()?.HasNext;
        set
        {
            var feature = Features.Get<IncrementalDataFeature>() ?? new IncrementalDataFeature();
            feature.HasNext = value;
            Features.Set(feature);
        }
    }

    /// <summary>
    /// Gets whether this result is incremental.
    /// </summary>
    public bool IsIncremental => Features.Get<IncrementalDataFeature>() is not null;

    /// <summary>
    /// Creates an operation result containing a single error.
    /// </summary>
    /// <param name="error">The error to include in the result.</param>
    /// <returns>An operation result containing the specified error.</returns>
    public static OperationResult FromError(IError error)
        => new([error]);

    /// <summary>
    /// Creates an operation result containing multiple errors.
    /// </summary>
    /// <param name="errors">The errors to include in the result.</param>
    /// <returns>An operation result containing the specified errors.</returns>
    public static OperationResult FromError(params ImmutableList<IError> errors)
        => new(errors);
}
