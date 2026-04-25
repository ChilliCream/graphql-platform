using CookieCrumble;

namespace Mocha.Resources.Tests;

public class MochaResourceSnapshotTests
{
    private const string InstanceId = "00000000-0000-0000-0000-000000000001";

    [Fact]
    public void Serialize_Should_MatchSnapshot_When_QueueResource()
    {
        // arrange
        var resource = new MochaQueueResource(
            system: "rabbitmq",
            transportId: "rabbitmq://localhost:5672/",
            name: "orders",
            address: "rabbitmq://localhost:5672/q/orders",
            durable: true,
            exclusive: false,
            autoDelete: false,
            autoProvision: true);

        // act
        var json = MochaResourceJsonWriter.Serialize(resource);

        // assert
        json.MatchSnapshot();
    }

    [Fact]
    public void Serialize_Should_MatchSnapshot_When_QueueResourceIsTemporary()
    {
        // arrange
        var resource = new MochaQueueResource(
            system: "rabbitmq",
            transportId: "rabbitmq://localhost:5672/",
            name: "amq.gen-replies-aaa",
            address: "rabbitmq://localhost:5672/q/amq.gen-replies-aaa",
            durable: false,
            exclusive: true,
            autoDelete: true,
            temporary: true);

        // act
        var json = MochaResourceJsonWriter.Serialize(resource);

        // assert
        json.MatchSnapshot();
    }

    [Fact]
    public void Serialize_Should_MatchSnapshot_When_ExchangeResource()
    {
        // arrange
        var resource = new MochaExchangeResource(
            system: "rabbitmq",
            transportId: "rabbitmq://localhost:5672/",
            name: "events",
            address: "rabbitmq://localhost:5672/e/events",
            exchangeType: "topic",
            durable: true,
            autoDelete: false,
            autoProvision: true);

        // act
        var json = MochaResourceJsonWriter.Serialize(resource);

        // assert
        json.MatchSnapshot();
    }

    [Fact]
    public void Serialize_Should_MatchSnapshot_When_TopicResource()
    {
        // arrange
        var resource = new MochaTopicResource(
            system: "memory",
            transportId: "memory://test/",
            name: "order-created",
            address: "memory://test/t/order-created",
            autoProvision: true);

        // act
        var json = MochaResourceJsonWriter.Serialize(resource);

        // assert
        json.MatchSnapshot();
    }

    [Fact]
    public void Serialize_Should_MatchSnapshot_When_BindingResource()
    {
        // arrange
        var resource = new MochaBindingResource(
            system: "rabbitmq",
            transportId: "rabbitmq://localhost:5672/",
            sourceName: "events",
            destinationName: "orders",
            sourceId: "urn:mocha:rabbitmq:exchange:rabbitmq%3A//localhost%3A5672/:events",
            destinationId: "urn:mocha:rabbitmq:queue:rabbitmq%3A//localhost%3A5672/:orders",
            routingKey: "order.*",
            address: "rabbitmq://localhost:5672/b/e/events/q/orders",
            autoProvision: true);

        // act
        var json = MochaResourceJsonWriter.Serialize(resource);

        // assert
        json.MatchSnapshot();
    }

    [Fact]
    public void Serialize_Should_MatchSnapshot_When_ServiceResource()
    {
        // arrange
        var resource = new MochaServiceResource(
            new HostDescription("OrderService", "Acme.OrderService", InstanceId));

        // act
        var json = MochaResourceJsonWriter.Serialize(resource);

        // assert
        json.MatchSnapshot();
    }

    [Fact]
    public void Serialize_Should_MatchSnapshot_When_MessageTypeResource()
    {
        // arrange
        var resource = new MochaMessageTypeResource(
            InstanceId,
            new MessageTypeDescription(
                Identity: "urn:message:Acme.Contracts:OrderPlaced",
                RuntimeType: "OrderPlaced",
                RuntimeTypeFullName: "Acme.Contracts.OrderPlaced",
                IsInterface: false,
                IsInternal: false,
                DefaultContentType: "application/json",
                EnclosedMessageIdentities: ["urn:message:Acme.Contracts:OrderItem"]));

        // act
        var json = MochaResourceJsonWriter.Serialize(resource);

        // assert
        json.MatchSnapshot();
    }

    [Fact]
    public void Serialize_Should_MatchSnapshot_When_HandlerResource()
    {
        // arrange
        var resource = new MochaHandlerResource(
            InstanceId,
            new ConsumerDescription(
                Name: "OrderPlacedHandler",
                IdentityType: "OrderPlacedHandler",
                IdentityTypeFullName: "Acme.OrderService.Handlers.OrderPlacedHandler",
                SagaName: null,
                IsBatch: false));

        // act
        var json = MochaResourceJsonWriter.Serialize(resource);

        // assert
        json.MatchSnapshot();
    }

    [Fact]
    public void Serialize_Should_MatchSnapshot_When_InboundRouteResource()
    {
        // arrange
        var resource = new MochaInboundRouteResource(
            InstanceId,
            index: 0,
            new InboundRouteDescription(
                Kind: InboundRouteKind.Subscribe,
                MessageTypeIdentity: "urn:message:Acme.Contracts:OrderPlaced",
                ConsumerName: "OrderPlacedHandler",
                Endpoint: new EndpointReferenceDescription(
                    Name: "orders",
                    Address: "rabbitmq://localhost:5672/q/orders",
                    TransportName: "RabbitMQ")));

        // act
        var json = MochaResourceJsonWriter.Serialize(resource);

        // assert
        json.MatchSnapshot();
    }

    [Fact]
    public void Serialize_Should_MatchSnapshot_When_OutboundRouteResource()
    {
        // arrange
        var resource = new MochaOutboundRouteResource(
            InstanceId,
            index: 0,
            new OutboundRouteDescription(
                Kind: OutboundRouteKind.Publish,
                MessageTypeIdentity: "urn:message:Acme.Contracts:OrderPlaced",
                Destination: "rabbitmq://localhost:5672/e/orders",
                Endpoint: new EndpointReferenceDescription(
                    Name: "orders",
                    Address: "rabbitmq://localhost:5672/e/orders",
                    TransportName: "RabbitMQ")));

        // act
        var json = MochaResourceJsonWriter.Serialize(resource);

        // assert
        json.MatchSnapshot();
    }

    [Fact]
    public void Serialize_Should_MatchSnapshot_When_TransportResource()
    {
        // arrange
        var resource = new MochaTransportResource(
            new TransportDescription(
                Identifier: "rabbitmq://localhost:5672/",
                Name: "RabbitMQ",
                Schema: "rabbitmq",
                TransportType: "RabbitMQMessagingTransport",
                ReceiveEndpoints: [],
                DispatchEndpoints: [],
                Topology: null));

        // act
        var json = MochaResourceJsonWriter.Serialize(resource);

        // assert
        json.MatchSnapshot();
    }

    [Fact]
    public void Serialize_Should_MatchSnapshot_When_ReceiveEndpointResource()
    {
        // arrange
        var resource = new MochaReceiveEndpointResource(
            system: "rabbitmq",
            transportId: "rabbitmq://localhost:5672/",
            new ReceiveEndpointDescription(
                Name: "orders",
                Kind: ReceiveEndpointKind.Default,
                Address: "rabbitmq://localhost:5672/q/orders",
                SourceAddress: "rabbitmq://localhost:5672/e/orders"));

        // act
        var json = MochaResourceJsonWriter.Serialize(resource);

        // assert
        json.MatchSnapshot();
    }

    [Fact]
    public void Serialize_Should_MatchSnapshot_When_DispatchEndpointResource()
    {
        // arrange
        var resource = new MochaDispatchEndpointResource(
            system: "rabbitmq",
            transportId: "rabbitmq://localhost:5672/",
            new DispatchEndpointDescription(
                Name: "orders",
                Kind: DispatchEndpointKind.Default,
                Address: "rabbitmq://localhost:5672/e/orders",
                DestinationAddress: "rabbitmq://localhost:5672/e/orders"));

        // act
        var json = MochaResourceJsonWriter.Serialize(resource);

        // assert
        json.MatchSnapshot();
    }

    [Fact]
    public void Serialize_Should_MatchSnapshot_When_TopologyEntityResource()
    {
        // arrange
        var resource = new MochaTopologyEntityResource(
            system: "memory",
            transportId: "memory://test/",
            new TopologyEntityDescription(
                Kind: "queue",
                Name: "orders",
                Address: "memory://test/q/orders",
                Flow: "outbound",
                Properties: null));

        // act
        var json = MochaResourceJsonWriter.Serialize(resource);

        // assert
        json.MatchSnapshot();
    }

    [Fact]
    public void Serialize_Should_MatchSnapshot_When_SagaResource()
    {
        // arrange
        var resource = new MochaSagaResource(
            InstanceId,
            new SagaDescription(
                Name: "OrderSaga",
                StateType: "OrderSagaState",
                StateTypeFullName: "Acme.OrderService.OrderSagaState",
                ConsumerName: "OrderSagaConsumer",
                States: []));

        // act
        var json = MochaResourceJsonWriter.Serialize(resource);

        // assert
        json.MatchSnapshot();
    }

    [Fact]
    public void Serialize_Should_MatchSnapshot_When_SagaStateResource()
    {
        // arrange
        var sagaId = MochaUrn.Create("core", "saga", InstanceId, "OrderSaga");
        var resource = new MochaSagaStateResource(
            sagaId,
            InstanceId,
            sagaName: "OrderSaga",
            new SagaStateDescription(
                Name: "AwaitingPayment",
                IsInitial: false,
                IsFinal: false,
                OnEntry: null,
                Response: null,
                Transitions: []));

        // act
        var json = MochaResourceJsonWriter.Serialize(resource);

        // assert
        json.MatchSnapshot();
    }

    [Fact]
    public void Serialize_Should_MatchSnapshot_When_SagaTransitionResource()
    {
        // arrange
        var sagaId = MochaUrn.Create("core", "saga", InstanceId, "OrderSaga");
        var fromStateId = MochaUrn.Create("core", "saga_state", InstanceId, "OrderSaga", "AwaitingPayment");
        var toStateId = MochaUrn.Create("core", "saga_state", InstanceId, "OrderSaga", "Completed");
        var resource = new MochaSagaTransitionResource(
            sagaId,
            fromStateId,
            toStateId,
            InstanceId,
            sagaName: "OrderSaga",
            fromStateName: "AwaitingPayment",
            index: 0,
            new SagaTransitionDescription(
                EventType: "PaymentReceived",
                EventTypeFullName: "Acme.Contracts.PaymentReceived",
                TransitionTo: "Completed",
                TransitionKind: Mocha.Sagas.SagaTransitionKind.Event,
                AutoProvision: false,
                Publish: null,
                Send: null));

        // act
        var json = MochaResourceJsonWriter.Serialize(resource);

        // assert
        json.MatchSnapshot();
    }

    [Fact]
    public void Serialize_Should_MatchSnapshot_When_SagaTransitionResourceHasPublishAndSend()
    {
        // arrange
        var sagaId = MochaUrn.Create("core", "saga", InstanceId, "OrderSaga");
        var fromStateId = MochaUrn.Create("core", "saga_state", InstanceId, "OrderSaga", "AwaitingPayment");
        var toStateId = MochaUrn.Create("core", "saga_state", InstanceId, "OrderSaga", "Completed");
        var resource = new MochaSagaTransitionResource(
            sagaId,
            fromStateId,
            toStateId,
            InstanceId,
            sagaName: "OrderSaga",
            fromStateName: "AwaitingPayment",
            index: 0,
            new SagaTransitionDescription(
                EventType: "PaymentReceived",
                EventTypeFullName: "Acme.Contracts.PaymentReceived",
                TransitionTo: "Completed",
                TransitionKind: Mocha.Sagas.SagaTransitionKind.Event,
                AutoProvision: false,
                Publish:
                [
                    new SagaEventDescription("OrderCompleted", "Acme.Contracts.OrderCompleted")
                ],
                Send:
                [
                    new SagaEventDescription("ShipOrder", null)
                ]));

        // act
        var json = MochaResourceJsonWriter.Serialize(resource);

        // assert
        json.MatchSnapshot();
    }
}
