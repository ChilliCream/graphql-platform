using HotChocolate.Caching.Memory;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

public sealed class FusionRequestOptions : ICloneable
{
    private static readonly TimeSpan s_minExecutionTimeout = TimeSpan.FromMilliseconds(100);
    private TimeSpan _executionTimeout = TimeSpan.FromSeconds(30);
    private int _operationExecutionPlanCacheSize = 256;
    private CacheDiagnostics? _operationExecutionPlanCacheDiagnostics;
    private int _operationDocumentCacheSize = 256;
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
        return clone;
    }

    object ICloneable.Clone() => Clone();

    internal void MakeReadOnly()
        => _isReadOnly = true;
}
