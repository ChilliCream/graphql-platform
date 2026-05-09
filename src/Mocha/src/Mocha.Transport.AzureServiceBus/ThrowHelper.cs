namespace Mocha.Transport.AzureServiceBus;

internal static class ThrowHelper
{
    // Connection
    public static Exception ConnectionStringOrCredentialRequired()
        => new InvalidOperationException(
            "Either ConnectionString or FullyQualifiedNamespace + Credential must be provided");

    // Convention
    public static Exception ReceiveEndpointQueueNameRequired()
        => new InvalidOperationException("Queue name is required");

    public static Exception DeadLetterForwardingConflict(
        string endpointName,
        string queueName,
        string existingForwardTarget)
        => new InvalidOperationException(
            $"Endpoint '{endpointName}' configured UseNativeDeadLetterForwarding() but "
            + $"queue '{queueName}' already forwards dead-lettered messages to "
            + $"'{existingForwardTarget}'. Choose one.");

    // ReceiveFeature
    public static Exception ReceiveFeatureArgsNotSet()
        => new InvalidOperationException("Receive feature has no args set");

    // Context
    public static Exception EndpointIsSessionBound()
        => new InvalidOperationException(
            "The current Azure Service Bus endpoint is session-bound. "
            + "Call GetAzureServiceBusSessionEventArgs() instead.");

    public static Exception ProcessMessageEventArgsUnavailable()
        => new InvalidOperationException(
            "The current message context is not running on a non-session Azure Service Bus endpoint. "
            + "ProcessMessageEventArgs is only available for handlers receiving from ASB.");

    public static Exception ProcessSessionMessageEventArgsUnavailable()
        => new InvalidOperationException(
            "The current message context is not running on a session-bound Azure Service Bus endpoint. "
            + "ProcessSessionMessageEventArgs is only available for handlers receiving from a session-required queue.");

    // ScheduledMessageStore
    public static Exception ScheduledMessageStoreRequiresAsbEndpoint(string actualEndpointType)
        => new InvalidOperationException(
            "AzureServiceBusScheduledMessageStore requires an AzureServiceBusDispatchEndpoint, "
            + $"but the dispatch context carries a '{actualEndpointType}'.");

    public static Exception ScheduledMessageStoreRequiresEnvelope()
        => new InvalidOperationException(
            "AzureServiceBusScheduledMessageStore requires a serialized envelope on the dispatch context.");

    public static Exception ScheduledMessageStoreRequiresScheduledTime()
        => new InvalidOperationException(
            "AzureServiceBusScheduledMessageStore requires the envelope to carry a scheduled time.");

    // Topology
    public static Exception TopicAlreadyExists(string topicName)
        => new InvalidOperationException($"Topic '{topicName}' already exists");

    public static Exception QueueAlreadyExists(string queueName)
        => new InvalidOperationException($"Queue '{queueName}' already exists");

    public static Exception TopologyTopicNotFound(string topicName)
        => new InvalidOperationException($"Topic '{topicName}' not found in topology");

    public static Exception TopologyQueueNotFound(string queueName)
        => new InvalidOperationException($"Queue '{queueName}' not found in topology");

    public static Exception SubscriptionAlreadyExists(string source, string destination)
        => new InvalidOperationException(
            $"Subscription from topic '{source}' to queue '{destination}' already exists");

    // DispatchEndpoint
    public static Exception DispatchEndpointTopicOrQueueNameRequired()
        => new InvalidOperationException("Topic name or queue name is required");

    public static Exception DispatchEndpointEnvelopeNotSet()
        => new InvalidOperationException("Envelope is not set");

    public static Exception DispatchEndpointDestinationAddressInvalidUri()
        => new InvalidOperationException("Destination address is not a valid URI");

    public static Exception DispatchEndpointCannotDetermineDestinationName(string destinationAddress)
        => new InvalidOperationException(
            $"Cannot determine topic or queue name from destination address {destinationAddress}");

    public static Exception DispatchEndpointDestinationNotConfigured()
        => new InvalidOperationException("Destination not configured");

    public static Exception PartitionKeyMustEqualSessionId()
        => new InvalidOperationException(
            "PartitionKey must equal SessionId when both are set on an Azure Service Bus message.");

    public static Exception DispatchEndpointTopicNotFound()
        => new InvalidOperationException("Topic not found");

    public static Exception DispatchEndpointQueueNotFound()
        => new InvalidOperationException("Queue not found");

    public static Exception DispatchEndpointDestinationNotSet()
        => new InvalidOperationException("Destination is not set");

    // ReceiveEndpoint
    public static Exception ReceiveEndpointQueueNotFound()
        => new InvalidOperationException("Queue not found");

    public static Exception ReceiveEndpointHasSessionOnlyOptionsOnNonSessionQueue(
        string endpointName,
        string queueName,
        string sessionOnlyOptionsList)
        => new InvalidOperationException(
            $"Receive endpoint '{endpointName}' targets queue '{queueName}', "
                + $"which does not require sessions ({nameof(AzureServiceBusQueue.RequiresSession)}=false). "
                + "The following session-only options are set and would have no effect: "
                + $"{sessionOnlyOptionsList}. Either remove these options, or call "
                + "WithRequiresSession() on the queue declaration.");

    public static Exception TransportIsNotAzureServiceBus()
        => new InvalidOperationException("Transport is not an AzureServiceBusMessagingTransport");
}
