using System.Diagnostics;
using HotChocolate.PersistedOperations;

namespace HotChocolate.Execution.Options;

/// <summary>
/// Represents the entirety of settings to configure the behavior of the
/// query execution engine.
/// </summary>
public class RequestExecutorOptions : IRequestExecutorOptionsAccessor
{
    private static readonly TimeSpan s_minExecutionTimeout = TimeSpan.FromMilliseconds(100);
    private TimeSpan _executionTimeout;

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
            _executionTimeout = value < s_minExecutionTimeout
                ? s_minExecutionTimeout
                : value;
        }
    }

    /// <summary>
    /// Gets or sets whether exception details should be included for GraphQL
    /// errors in the GraphQL response.
    /// <see cref="Debugger.IsAttached"/> by default.
    /// </summary>
    /// <remarks>
    /// When set to <c>true</c> includes the message and stack trace of exceptions
    /// in the user-facing GraphQL error.
    /// Since this could leak security-critical information, this option should only
    /// be set to <c>true</c> for development purposes and not in production environments.
    /// </remarks>
    public bool IncludeExceptionDetails { get; set; } = Debugger.IsAttached;

    /// <summary>
    /// Specifies the behavior of the persisted operation pipeline.
    /// </summary>
    public PersistedOperationOptions PersistedOperations
    {
        get;
        set
        {
            ArgumentNullException.ThrowIfNull(value, nameof(PersistedOperationOptions));
            field = value;
        }
    } = new();
}
