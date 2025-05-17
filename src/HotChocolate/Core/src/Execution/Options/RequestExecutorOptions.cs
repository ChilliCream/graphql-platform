using System.Diagnostics;
using static HotChocolate.Execution.Options.PersistedOperationOptions;

namespace HotChocolate.Execution.Options;

/// <summary>
/// Represents the entirety of settings to configure the behavior of the
/// query execution engine.
/// </summary>
public class RequestExecutorOptions : IRequestExecutorOptionsAccessor
{
    private static readonly TimeSpan _minExecutionTimeout = TimeSpan.FromMilliseconds(100);
    private TimeSpan _executionTimeout;
    private PersistedOperationOptions _persistedOperations = new();
    private int _streamBufferSize;

    /// <summary>
    /// <para>Initializes a new instance of <see cref="RequestExecutorOptions"/>.</para>
    /// <para>
    /// If the debugger is attached (<see cref="Debugger.IsAttached"/>) new instances will be
    /// initialized with a default <see cref="ExecutionTimeout"/> of 30 minutes; otherwise, the
    /// default <see cref="ExecutionTimeout"/> will be 30 seconds.
    /// </para>
    /// </summary>
    public RequestExecutorOptions()
    {
        _executionTimeout = Debugger.IsAttached
            ? TimeSpan.FromMinutes(30)
            : TimeSpan.FromSeconds(30);
        _streamBufferSize = 100;
    }

    /// <summary>
    /// Gets or sets maximum allowed execution time of a query.
    /// The minimum allowed value is <c>100</c> milliseconds.
    /// </summary>
    public TimeSpan ExecutionTimeout
    {
        get => _executionTimeout;
        set
        {
            _executionTimeout = value < _minExecutionTimeout
                ? _minExecutionTimeout
                : value;
        }
    }

    /// <summary>
    /// <para>
    /// Gets or sets a value indicating whether the <c>GraphQL</c> errors
    /// should be extended with exception details.
    /// </para>
    /// <para>The default value is <see cref="Debugger.IsAttached"/>.</para>
    /// </summary>
    public bool IncludeExceptionDetails { get; set; } = Debugger.IsAttached;

    /// <summary>
    /// Specifies that the transport is allowed to provide the schema SDL document as a file.
    /// </summary>
    public bool EnableSchemaFileSupport { get; set; } = true;

    /// <summary>
    /// Specifies the behavior of the persisted operation pipeline.
    /// </summary>
    public PersistedOperationOptions PersistedOperations
    {
        get => _persistedOperations;
        set
        {
            _persistedOperations = value
                ?? throw new ArgumentNullException(nameof(PersistedOperations));
        }
    }

    /// <summary>
    /// Gets or sets maximum allowed number of items that can be
    /// prefetched from the data source and buffered while streaming.
    /// The minimum allowed value is <c>1</c> item.
    /// </summary>
    public int StreamBufferSize
    {
        get => _streamBufferSize;
        set
        {
            _streamBufferSize = value >= 1 ? value : 1; 
        }
    }
}
