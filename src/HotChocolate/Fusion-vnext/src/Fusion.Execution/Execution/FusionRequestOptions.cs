using System.Diagnostics;
using HotChocolate.PersistedOperations;

namespace HotChocolate.Fusion.Execution;

public sealed class FusionRequestOptions : ICloneable
{
    private static readonly TimeSpan s_minExecutionTimeout = TimeSpan.FromMilliseconds(100);
    private bool _isReadOnly;

    /// <summary>
    /// Gets or sets the execution timeout.
    /// <c>30s</c> by default. <c>100ms</c> is the minimum.
    /// </summary>
    public TimeSpan ExecutionTimeout
    {
        get;
        set
        {
            ExpectMutableOptions();

            field = value < s_minExecutionTimeout
                ? s_minExecutionTimeout
                : value;
        }
    } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets whether telemetry data like status and duration
    /// of operation plan nodes should be collected.
    /// <c>false</c> by default.
    /// </summary>
    public bool CollectOperationPlanTelemetry
    {
        get;
        set
        {
            ExpectMutableOptions();

            field = value;
        }
    }

    /// <summary>
    /// Gets or sets whether the <see cref="FusionOptions.DefaultErrorHandlingMode"/> can be overriden
    /// on a per-request basis.
    /// <c>false</c> by default.
    /// </summary>
    public bool AllowErrorHandlingModeOverride
    {
        get;
        set
        {
            ExpectMutableOptions();

            field = value;
        }
    }

    /// <summary>
    /// Gets or sets the persisted operation options.
    /// </summary>
    public PersistedOperationOptions PersistedOperations
    {
        get;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            ExpectMutableOptions();

            field = value;
        }
    } = new();

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
    public bool IncludeExceptionDetails
    {
        get;
        set
        {
            ExpectMutableOptions();

            field = value;
        }
    } = Debugger.IsAttached;

    /// <summary>
    /// Clones the request options into a new mutable instance.
    /// </summary>
    /// <returns>
    /// A new mutable instance of <see cref="FusionRequestOptions"/> with the same properties.
    /// </returns>
    public FusionRequestOptions Clone()
    {
        return new FusionRequestOptions
        {
            ExecutionTimeout = ExecutionTimeout,
            CollectOperationPlanTelemetry = CollectOperationPlanTelemetry,
            AllowErrorHandlingModeOverride = AllowErrorHandlingModeOverride,
            PersistedOperations = PersistedOperations,
            IncludeExceptionDetails = IncludeExceptionDetails
        };
    }

    object ICloneable.Clone() => Clone();

    internal void MakeReadOnly()
        => _isReadOnly = true;

    private void ExpectMutableOptions()
    {
        if (_isReadOnly)
        {
            throw new InvalidOperationException("The request options are read-only.");
        }
    }
}
