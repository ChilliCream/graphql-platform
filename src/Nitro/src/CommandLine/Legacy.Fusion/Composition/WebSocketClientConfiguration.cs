namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Represents the configuration for a WebSocket client that can be used to subscribe to a subgraph.
/// </summary>
public sealed class WebSocketClientConfiguration : IClientConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WebSocketClientConfiguration"/> class.
    /// </summary>
    /// <param name="baseAddress">
    /// The base address of the client.
    /// </param>
    /// <param name="clientName">
    /// The name of the client.
    /// </param>
    public WebSocketClientConfiguration(Uri baseAddress, string? clientName = null)
    {
        BaseAddress = baseAddress;
        ClientName = clientName;
    }

    /// <summary>
    /// Gets the name of the client.
    /// </summary>
    public string? ClientName { get; }

    /// <summary>
    /// Gets the base address of the client.
    /// </summary>
    public Uri BaseAddress { get; }
}
