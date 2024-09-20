using System.Diagnostics;

namespace HotChocolate.Execution.Options;

/// <summary>
/// Represents the entirety of settings to configure the behavior of the
/// query execution engine.
/// </summary>
public class RequestExecutorOptions : IRequestExecutorOptionsAccessor
{
    private static readonly TimeSpan _minExecutionTimeout = TimeSpan.FromMilliseconds(100);
    private TimeSpan _executionTimeout;
    private IError _onlyPersistedOperationsAreAllowedError = ErrorHelper.OnlyPersistedOperationsAreAllowed();

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
    /// Gets or sets a value indicating whether the <c>GraphQL</c> errors
    /// should be extended with exception details.
    ///
    /// The default value is <see cref="Debugger.IsAttached"/>.
    /// </summary>
    public bool IncludeExceptionDetails { get; set; } = Debugger.IsAttached;

    /// <summary>
    /// Specifies if only persisted operations are allowed when using
    /// the persisted operation pipeline.
    ///
    /// The default is <c>false</c>.
    /// </summary>
    public bool OnlyAllowPersistedOperations { get; set; } = false;

    /// <summary>
    /// The error that will be thrown when only persisted
    /// operations are allowed and a normal operation is issued.
    /// </summary>
    public IError OnlyPersistedOperationsAreAllowedError
    {
        get => _onlyPersistedOperationsAreAllowedError;
        set
        {
            _onlyPersistedOperationsAreAllowedError = value
                ?? throw new ArgumentNullException(
                    nameof(OnlyPersistedOperationsAreAllowedError));
        }
    }

    /// <summary>
    /// Specifies that the transport is allowed to provide the schema SDL document as a file.
    /// </summary>
    public bool EnableSchemaFileSupport { get; set; } = true;
}
