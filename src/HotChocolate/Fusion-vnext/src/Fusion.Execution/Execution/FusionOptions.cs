using HotChocolate.Caching.Memory;
using HotChocolate.Execution.Relay;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

public sealed class FusionOptions : IFusionSchemaOptions, ICloneable
{
    private bool _isReadOnly;

    /// <summary>
    /// Gets or sets the time that the executor manager waits to dispose the schema services.
    /// <c>30s</c> by default.
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

            if (value < 16)
            {
                throw new ArgumentException(
                    "The size of operation execution plan cache must be at least 16.");
            }

            field = value;
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

            if (value < 16)
            {
                throw new ArgumentException(
                    "The size of operation document cache must be at least 16.");
            }

            field = value;
        }
    } = 256;

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
    /// Gets or sets whether the request executor should be initialized lazily.
    /// <c>false</c> by default.
    /// </summary>
    /// <remarks>
    /// When set to <c>true</c> the creation of the schema and request executor, as well as
    /// the load of the Fusion configuration, is deferred until the request executor
    /// is first requested.
    /// This can significantly slow down and block initial requests.
    /// Therefore, it is recommended to not use this option for production environments.
    /// </remarks>
    public bool LazyInitialization
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
    /// Applies the @serializeAs directive to scalar types that specify a serialization format.
    /// </summary>
    public bool ApplySerializeAsToScalars
    {
        get;
        set
        {
            ExpectMutableOptions();

            field = value;
        }
    }

    /// <summary>
    /// Clones the options into a new mutable instance.
    /// </summary>
    /// <returns>
    /// A new mutable instance of <see cref="FusionOptions"/> with the same properties.
    /// </returns>
    public FusionOptions Clone()
    {
        return new FusionOptions
        {
            EvictionTimeout = EvictionTimeout,
            OperationExecutionPlanCacheSize = OperationExecutionPlanCacheSize,
            OperationExecutionPlanCacheDiagnostics = OperationExecutionPlanCacheDiagnostics,
            OperationDocumentCacheSize = OperationDocumentCacheSize,
            DefaultErrorHandlingMode = DefaultErrorHandlingMode,
            LazyInitialization = LazyInitialization,
            NodeIdSerializerFormat = NodeIdSerializerFormat,
            ApplySerializeAsToScalars = ApplySerializeAsToScalars
        };
    }

    object ICloneable.Clone() => Clone();

    internal void MakeReadOnly()
        => _isReadOnly = true;

    private void ExpectMutableOptions()
    {
        if (_isReadOnly)
        {
            throw new InvalidOperationException("The options are read-only.");
        }
    }
}
