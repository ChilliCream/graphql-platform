namespace Mocha.Features;

/// <summary>
/// Configures where faulted messages are forwarded for a receive endpoint.
/// </summary>
public sealed class ReceiveFaultEndpointFeature
{
    /// <summary>
    /// Gets or sets the address of the fault endpoint.
    /// </summary>
    public Uri? Address { get; set; }

    /// <summary>
    /// Gets or sets the queue name of the fault endpoint.
    /// </summary>
    public string? QueueName { get; set; }

    /// <summary>
    /// Gets or sets the dispatch endpoint to forward faulted messages to.
    /// </summary>
    public DispatchEndpoint? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether fault forwarding is disabled.
    /// </summary>
    public bool IsDisabled { get; set; }
}
