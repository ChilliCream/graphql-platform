namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Convention that ensures topics exist in the topology for dispatch endpoints
/// creating a topic entry for the endpoint's hub name if it does not already exist.
/// </summary>
public sealed class EventHubDispatchEndpointTopologyConvention : IEventHubDispatchEndpointTopologyConvention
{
    /// <inheritdoc />
    public void DiscoverTopology(
        IMessagingConfigurationContext context,
        EventHubDispatchEndpoint endpoint,
        EventHubDispatchEndpointConfiguration configuration)
    {
        var topology = (EventHubMessagingTopology)endpoint.Transport.Topology;

        if (configuration.HubName is not null
            && topology.Topics.FirstOrDefault(t => t.Name == configuration.HubName) is null)
        {
            topology.AddTopic(new EventHubTopicConfiguration { Name = configuration.HubName });
        }
    }
}
