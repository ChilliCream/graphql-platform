using HotChocolate.Caching.Memory;

namespace HotChocolate.Fusion.Execution;

public sealed class FusionRequestOptions : ICloneable
{
    private static readonly TimeSpan s_minExecutionTimeout = TimeSpan.FromMilliseconds(100);
    private TimeSpan _executionTimeout = TimeSpan.FromSeconds(30);
    private int _operationExecutionPlanCacheSize = 256;
    private CacheDiagnostics? _operationExecutionPlanCacheDiagnostics;
    private int _operationDocumentCacheSize = 256;
    private bool _collectOperationPlanTelemetry;
    private bool _allowOperationPlanRequests;
    private bool _allowErrorHandlingOverride;
    private ErrorHandlingMode _errorHandlingMode = ErrorHandlingMode.Propagate;
    private bool _isReadOnly;

    /// <summary>
    /// Gets or sets the execution timeout.
    /// By default, the execution timeout is set to 30 seconds;
    /// </summary>
    public TimeSpan ExecutionTimeout
    {
        get => _executionTimeout;
        set
        {
            if (_isReadOnly)
            {
                throw new InvalidOperationException("The request options are read-only.");
            }

            _executionTimeout = value < s_minExecutionTimeout
                ? s_minExecutionTimeout
                : value;
        }
    }

    /// <summary>
    /// Gets or sets the size of the operation execution plan cache.
    /// By default, the cache will store up to 256 operation execution plans.
    /// </summary>
    public int OperationExecutionPlanCacheSize
    {
        get => _operationExecutionPlanCacheSize;
        set
        {
            if (_isReadOnly)
            {
                throw new InvalidOperationException("The request options are read-only.");
            }

            _operationExecutionPlanCacheSize = value;
        }
    }

    /// <summary>
    /// Gets or sets the diagnostics for the operation execution plan cache.
    /// </summary>
    public CacheDiagnostics? OperationExecutionPlanCacheDiagnostics
    {
        get => _operationExecutionPlanCacheDiagnostics;
        set
        {
            if (_isReadOnly)
            {
                throw new InvalidOperationException("The request options are read-only.");
            }

            _operationExecutionPlanCacheDiagnostics = value;
        }
    }

    /// <summary>
    /// Gets or sets the size of the operation document cache.
    /// By default, the cache will store up to 256 operation documents.
    /// </summary>
    public int OperationDocumentCacheSize
    {
        get => _operationDocumentCacheSize;
        set
        {
            if (_isReadOnly)
            {
                throw new InvalidOperationException("The request options are read-only.");
            }

            _operationDocumentCacheSize = value;
        }
    }

    /// <summary>
    /// Gets or sets whether telemetry data like status and duration
    /// of operation plan nodes should be collected.
    /// <c>false</c> by default.
    /// </summary>
    public bool CollectOperationPlanTelemetry
    {
        get => _collectOperationPlanTelemetry;
        set
        {
            if (_isReadOnly)
            {
                throw new InvalidOperationException("The request options are read-only.");
            }

            _collectOperationPlanTelemetry = value;
        }
    }

    /// <summary>
    /// Gets or sets whether the operation plan can be requested via the <c>Fusion-Operation-Plan</c> header.
    /// <c>false</c> by default.
    /// </summary>
    // TODO: Better name
    public bool AllowOperationPlanRequests
    {
        get => _allowOperationPlanRequests;
        set
        {
            if (_isReadOnly)
            {
                throw new InvalidOperationException("The request options are read-only.");
            }

            _allowOperationPlanRequests = value;
        }
    }

    /// <summary>
    /// Gets or sets whether the operation plan can be requested via the <c>GraphQL-Error-Handling</c> header.
    /// <c>false</c> by default.
    /// </summary>
    // TODO: Better name
    public ErrorHandlingMode ErrorHandlingMode
    {
        get => _errorHandlingMode;
        set
        {
            if (_isReadOnly)
            {
                throw new InvalidOperationException("The request options are read-only.");
            }

            _errorHandlingMode = value;
        }
    }

    /// <summary>
    /// Gets or sets whether the operation plan can be requested via the <c>GraphQL-Error-Handling</c> header.
    /// <c>false</c> by default.
    /// </summary>
    // TODO: Better name
    public bool AllowErrorHandlingOverride
    {
        get => _allowErrorHandlingOverride;
        set
        {
            if (_isReadOnly)
            {
                throw new InvalidOperationException("The request options are read-only.");
            }

            _allowErrorHandlingOverride = value;
        }
    }

    /// <summary>
    /// Clones the request options into a new mutable instance.
    /// </summary>
    /// <returns>
    /// A new mutable instance of <see cref="FusionRequestOptions"/> with the same properties.
    /// </returns>
    public FusionRequestOptions Clone()
    {
        var clone = new FusionRequestOptions();
        clone._executionTimeout = _executionTimeout;
        clone._operationExecutionPlanCacheSize = _operationExecutionPlanCacheSize;
        clone._operationExecutionPlanCacheDiagnostics = _operationExecutionPlanCacheDiagnostics;
        clone._operationDocumentCacheSize = _operationDocumentCacheSize;
        clone._collectOperationPlanTelemetry = _collectOperationPlanTelemetry;
        clone._allowOperationPlanRequests = _allowOperationPlanRequests;
        return clone;
    }

    object ICloneable.Clone() => Clone();

    internal void MakeReadOnly()
        => _isReadOnly = true;
}
