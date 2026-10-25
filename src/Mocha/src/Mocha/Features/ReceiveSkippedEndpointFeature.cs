namespace Mocha.Features;

/// <summary>
/// Configures where skipped or unconsumed messages are forwarded for a receive endpoint.
/// </summary>
public sealed class ReceiveSkippedEndpointFeature
{
    /// <summary>
    /// Gets or sets the absolute address used for forwarding skipped messages.
    /// </summary>
    public Uri? Address { get; set; }

    /// <summary>
    /// Gets or sets the dispatch endpoint used to route skipped messages.
    /// </summary>
    public DispatchEndpoint? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether forwarding skipped messages is disabled.
    /// </summary>
    public bool IsDisabled { get; set; }
}
