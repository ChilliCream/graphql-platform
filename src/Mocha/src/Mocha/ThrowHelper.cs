namespace Mocha;

internal static class ThrowHelper
{
    public static Exception BeforeAndAfterConflict()
        => new ArgumentException(
            "Only one of 'before' or 'after' can be specified at the same time.");

    public static Exception MiddlewareKeyNotFound(string key)
        => new InvalidOperationException($"Middleware with key {key} not found");

    public static Exception RouteEndpointNotConnected()
        => new InvalidOperationException("Endpoint is not connected");

    public static Exception RouteMustNotBeInitialized()
        => new InvalidOperationException("Route must not be initialized");

    public static Exception RouteMustBeInitialized()
        => new InvalidOperationException("Route must be initialized");

    public static Exception RouteMustNotBeCompleted()
        => new InvalidOperationException("Route must not be completed");

    public static Exception RouteRequiresMessageType()
        => new InvalidOperationException("Route requires a message type");

    public static Exception RouteRequiresConsumer()
        => new InvalidOperationException("Route requires a consumer");

    public static Exception RouteNotInitialized()
        => new InvalidOperationException("Route is not initialized");

    public static Exception NoHandlerForMessageType(Type messageType, string? endpointName)
        => new InvalidOperationException(
            $"No handler or consumer handles message type '{messageType.FullName}' "
            + $"declared on receive endpoint '{endpointName}'.");

    public static Exception TransportConfigurationMissing()
        => new InvalidOperationException("Could not create configuration for transport");

    public static Exception TransportNameRequired()
        => new InvalidOperationException("Transport name is required");

    public static Exception TransportSchemaRequired()
        => new InvalidOperationException("Transport schema is required");

    public static Exception TransportAlreadyStarted()
        => new InvalidOperationException("Transport is already started");

    public static Exception TransportNotStarted()
        => new InvalidOperationException("Transport is not started");

    public static Exception EndpointConfigurationFailed()
        => new InvalidOperationException("Failed to create endpoint configuration");

    public static Exception ReplyConsumerNotFound()
        => new InvalidOperationException("Reply consumer not found");

    public static Exception FeaturesNotInitialized()
        => new InvalidOperationException("Features are not initialized");

    public static Exception ConsumerNameRequired()
        => new InvalidOperationException("Consumer name is null");

    public static Exception HandlerConfigurationMissing()
        => new InvalidOperationException("Handler configuration is null");

    public static Exception HandlerAlreadyInitialized()
        => new InvalidOperationException("Handler already initialized");

    public static Exception InvalidHandlerContext()
        => new InvalidOperationException("Context is not a handler context");

    public static Exception EndpointNameRequired()
        => new InvalidOperationException("Name is required");

    public static Exception EndpointAlreadyInitialized()
        => new InvalidOperationException("Endpoint already initialized");

    public static Exception NoTransportForAddress(string address)
        => new InvalidOperationException($"No transport can handle address: {address}");

    public static Exception EndpointMustBeRegistered()
        => new InvalidOperationException("Endpoint must be registered before adding addresses");

    public static Exception NoTransportForMessageType(MessageType messageType)
        => new InvalidOperationException($"No transport can handle message type: {messageType}");

    public static Exception TransportNotFoundForAddress(string address)
        => new InvalidOperationException($"Transport not found for address {address}");

    public static Exception ReplyDispatchEndpointNotFound(string address)
        => new InvalidOperationException($"Reply dispatch endpoint not found for address {address}");

    public static Exception NoReplyEndpointFound(string endpoint)
        => new InvalidOperationException($"No reply endpoint was found for {endpoint}");

    public static Exception UnexpectedResponseType()
        => new InvalidOperationException("Unexpected response type.");

    public static Exception ResponseIsNull()
        => new InvalidOperationException("Response is null.");

    public static Exception ResponseBodyNotSet()
        => new InvalidOperationException("Response body is not set. Could not be parsed.");

    public static Exception PromiseNotFound()
        => new InvalidOperationException("Promise with correlation ID not found.");

    public static Exception EnvelopeRequired()
        => new InvalidOperationException("Envelope is required for deserialization");

    public static Exception MessageTypeRequired()
        => new InvalidOperationException("Message type is required for deserialization");

    public static Exception ContentTypeRequired()
        => new InvalidOperationException("Content type is required for deserialization");

    public static Exception SerializerNotFound(string messageType, string contentType)
        => new InvalidOperationException(
            $"No serializer was found for message type {messageType} and content type {contentType}");

    public static Exception CouldNotDeserializeMessage()
        => new InvalidOperationException("Could not deserialize message");

    public static Exception DispatchMessageRequired()
        => new InvalidOperationException(
            "To send a message either the body must be set or the message must be set");

    public static Exception DispatchMessageTypeRequired()
        => new InvalidOperationException(
            "To send a message a message type must be set. Otherwise there is no way to serialize the message");

    public static Exception DispatchContentTypeRequired()
        => new InvalidOperationException(
            "To send a message a content type must be set. Otherwise there is no way to serialize the message");

    public static Exception DispatchSerializerNotFound(string contentType, string messageType)
        => new InvalidOperationException(
            $"No serializer found for content type {contentType} and message type {messageType}");

    public static Exception TopologyRequired()
        => new InvalidOperationException("Topology is required");

    public static Exception MessagingRuntimeNotInitialized()
        => new InvalidOperationException(
            "Messaging runtime is not initialized, you can only access the runtime after it has been built.");

    public static Exception FailedToCreateConsumer(Type consumerType)
        => new InvalidOperationException($"Failed to create consumer for type {consumerType}");

    public static Exception InvalidHandlerType()
        => new InvalidOperationException(
            "Handler type must be either an event handler, a request handler, or both.");

    public static Exception NoRootServicesFound()
        => new InvalidOperationException("No root services found");

    public static Exception SagaNotInitialized()
        => new InvalidOperationException("Saga is not initialized.");

    public static Exception SagaEventIsRequest(Type eventType)
        => new InvalidOperationException(
            $"Event type '{eventType}' is a request and should be handled with 'OnRequest' method.");

    public static Exception FeatureIsReadOnly()
        => new InvalidOperationException("The feature is read-only.");

    public static Exception HostDescriptionMissing()
        => new InvalidOperationException("Host description is missing.");

    public static Exception ReceivesReplyType(string messageTypeName)
        => new InvalidOperationException(
            $"Message type '{messageTypeName}' is a reply type and cannot be declared with Receives<T>. "
            + "Reply routes are address-routed and not subject to auto-binding topology.");

    public static Exception ReceivesClaimedType(string messageTypeName, string claimingEndpoint, string conflictingEndpoint)
        => new InvalidOperationException(
            $"Message type '{messageTypeName}' is already claimed by receive endpoint '{claimingEndpoint}' "
            + $"and cannot be claimed by endpoint '{conflictingEndpoint}'. Each message type can be claimed by at most one endpoint.");

    public static Exception BindFromWithNonNullRoutingKey(string transportName, string messageTypeName, string queueName)
        => new InvalidOperationException(
            $"Message type '{messageTypeName}' on {transportName} transport cannot have a non-null routing key "
            + $"in BindFrom binding to queue '{queueName}'. The {transportName} transport does not support routing keys.");

    public static Exception TwoReceiveEndpointsShareOneQueue(string queueName, string endpoint1, string endpoint2)
        => new InvalidOperationException(
            $"Queue '{queueName}' is claimed by both receive endpoint '{endpoint1}' and receive endpoint '{endpoint2}'. "
            + "Each queue can have at most one receive endpoint. Configure it via 't.Queue(name)...' "
            + "and declare extra queue shape via 't.DeclareQueue(name)' if needed.");

    public static Exception FaultOrSkippedQueueRequiresConsumingEndpoint(string queueType, string queueName)
        => new InvalidOperationException(
            $"The {queueType} queue for '{queueName}' cannot be configured on an entity-only queue. "
            + "Error and skipped queues can only be configured on a receive endpoint with at least one consumer or Receives<T> declaration. "
            + "Add a consumer via 'Handler<T>', 'Consumer<T>', or 'Receives<T>' to make this a consuming endpoint.");
}
