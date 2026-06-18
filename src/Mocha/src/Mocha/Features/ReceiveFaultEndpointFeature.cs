namespace Mocha.Features;

/// <summary>
/// Configures where faulted messages are forwarded for a receive endpoint.
/// </summary>
public sealed class ReceiveFaultEndpointFeature
{
    public Uri? Address { get; set; }

    public string? QueueName { get; set; }

    public DispatchEndpoint? Endpoint { get; set; }

    public bool IsDisabled { get; set; }
}
