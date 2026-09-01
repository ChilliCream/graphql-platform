namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Represents the configuration for fetching data from a source schema over WebSocket.
/// </summary>
public class WebSocketSourceSchemaClientConfiguration : ISourceSchemaClientConfiguration
{
    /// <summary>
    /// The default interval between WebSocket keep-alive frames.
    /// </summary>
    public static readonly TimeSpan DefaultKeepAliveInterval = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The default maximum number of response payload bytes queued for one operation.
    /// </summary>
    public const int DefaultMaxOperationQueueBytes = 1024 * 1024;

    /// <summary>
    /// Initializes a source schema WebSocket configuration.
    /// </summary>
    /// <param name="name">The name of the source schema.</param>
    /// <param name="url">The WebSocket endpoint of the source schema.</param>
    /// <param name="supportedOperations">The operation types routed through this client.</param>
    /// <param name="capabilities">The batching capabilities of the source schema.</param>
    /// <param name="keepAliveInterval">The interval between WebSocket keep-alive frames.</param>
    /// <param name="maxOperationQueueBytes">
    /// The maximum number of response payload bytes queued for one operation.
    /// </param>
    public WebSocketSourceSchemaClientConfiguration(
        string name,
        Uri url,
        SupportedOperationType supportedOperations = SupportedOperationType.All,
        SourceSchemaClientCapabilities capabilities = SourceSchemaClientCapabilities.Default,
        TimeSpan? keepAliveInterval = null,
        int maxOperationQueueBytes = DefaultMaxOperationQueueBytes)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(url);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxOperationQueueBytes);

        var resolvedKeepAliveInterval = keepAliveInterval ?? DefaultKeepAliveInterval;
        ArgumentOutOfRangeException.ThrowIfLessThan(
            resolvedKeepAliveInterval,
            TimeSpan.Zero,
            nameof(keepAliveInterval));

        Name = name;
        Url = url;
        SupportedOperations = supportedOperations;
        Capabilities = capabilities;
        KeepAliveInterval = resolvedKeepAliveInterval;
        MaxOperationQueueBytes = maxOperationQueueBytes;
    }

    /// <summary>
    /// Gets the name of the source schema.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the WebSocket endpoint of the source schema.
    /// </summary>
    public Uri Url { get; }

    /// <summary>
    /// Gets the operation types routed through this client.
    /// </summary>
    public SupportedOperationType SupportedOperations { get; }

    /// <summary>
    /// Gets the batching capabilities of the source schema.
    /// </summary>
    public SourceSchemaClientCapabilities Capabilities { get; }

    /// <summary>
    /// Gets the interval between WebSocket keep-alive frames.
    /// </summary>
    public TimeSpan KeepAliveInterval { get; }

    /// <summary>
    /// Gets the maximum number of response payload bytes queued for one operation.
    /// </summary>
    public int MaxOperationQueueBytes { get; }
}
