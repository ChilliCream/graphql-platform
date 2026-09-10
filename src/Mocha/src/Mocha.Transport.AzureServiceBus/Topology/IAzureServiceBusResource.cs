namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Represents an Azure Service Bus topology resource that can be provisioned on the broker.
/// </summary>
public interface IAzureServiceBusResource
{
    /// <summary>
    /// Provisions this resource in Azure Service Bus using the administration client.
    /// </summary>
    Task ProvisionAsync(
        AzureServiceBusClientManager clientManager,
        CancellationToken cancellationToken);
}
