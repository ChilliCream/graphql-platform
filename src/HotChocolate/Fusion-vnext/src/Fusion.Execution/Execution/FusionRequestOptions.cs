using HotChocolate.Caching.Memory;
using HotChocolate.Execution.Relay;
using HotChocolate.Language;
using HotChocolate.PersistedOperations;

namespace HotChocolate.Fusion.Execution;

public sealed class FusionRequestOptions : ICloneable
{
    private static readonly TimeSpan s_minExecutionTimeout = TimeSpan.FromMilliseconds(100);
    private bool _isReadOnly;

    /// <summary>
    /// Gets or sets the execution timeout.
    /// 30 seconds by default.
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
    /// Gets or sets the time that the executor manager waits to dispose the schema services.
    /// 30 seconds by default.
    /// </summary>
    public TimeSpan EvictionTimeout
    {
        get;
        set
        {
            ExpectMutableOptions();

            field = value;
        }
    } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the size of the operation execution plan cache.
    /// <c>256</c> by default. <c>16</c> is the minimum.
    /// </summary>
    public int OperationExecutionPlanCacheSize
    {
        get;
        set
        {
            ExpectMutableOptions();

            field = value < 16
                ? 16
                : value;
        }
    } = 256;

    /// <summary>
    /// Gets or sets the diagnostics for the operation execution plan cache.
    /// </summary>
    public CacheDiagnostics? OperationExecutionPlanCacheDiagnostics
    {
        get;
        set
        {
            ExpectMutableOptions();

            field = value;
        }
    }

    /// <summary>
    /// Gets or sets the size of the operation document cache.
    /// <c>256</c> by default. <c>16</c> is the minimum.
    /// </summary>
    public int OperationDocumentCacheSize
    {
        get;
        set
        {
            ExpectMutableOptions();

            field = value < 16
                ? 16
                : value;
        }
    } = 256;

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
    /// Gets or sets the default error handling mode.
    /// <see cref="ErrorHandlingMode.Propagate"/> by default.
    /// </summary>
    public ErrorHandlingMode DefaultErrorHandlingMode
    {
        get;
        set
        {
            ExpectMutableOptions();

            field = value;
        }
    } = ErrorHandlingMode.Propagate;

    /// <summary>
    /// Gets or sets whether the <see cref="DefaultErrorHandlingMode"/> can be overriden
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
    /// This should only be enabled for development purposes
    /// and not in production environments.
    /// <c>false</c> by default.
    /// </summary>
    public bool IncludeExceptionDetails
    {
        get;
        set
        {
            ExpectMutableOptions();

            field = value;
        }
    }

    /// <summary>
    /// Specifies the format for Global Object Identifiers.
    /// <see cref="NodeIdSerializerFormat.Base64"/> by default.
    /// </summary>
    public NodeIdSerializerFormat NodeIdSerializerFormat
    {
        get;
        set
        {
            ExpectMutableOptions();

            field = value;
        }
    } = NodeIdSerializerFormat.Base64;

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
            EvictionTimeout = EvictionTimeout,
            OperationExecutionPlanCacheSize = OperationExecutionPlanCacheSize,
            OperationExecutionPlanCacheDiagnostics = OperationExecutionPlanCacheDiagnostics,
            OperationDocumentCacheSize = OperationDocumentCacheSize,
            CollectOperationPlanTelemetry = CollectOperationPlanTelemetry,
            DefaultErrorHandlingMode = DefaultErrorHandlingMode,
            AllowErrorHandlingModeOverride = AllowErrorHandlingModeOverride,
            PersistedOperations = PersistedOperations,
            IncludeExceptionDetails = IncludeExceptionDetails,
            NodeIdSerializerFormat = NodeIdSerializerFormat
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
