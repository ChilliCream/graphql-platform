using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Tests;

public class SerializationTests
{
    [Fact]
    public void SerializerRegistry_Should_Exist_When_RuntimeIsCreated()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        Assert.NotNull(runtime.Messages.Serializers);
    }

    [Fact]
    public void GetSerializer_Should_ReturnJsonSerializer_When_QueryingMessageType()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated));
        Assert.NotNull(messageType);

        var serializer = messageType.GetSerializer(MessageContentType.Json);
        Assert.NotNull(serializer);
        Assert.Equal(MessageContentType.Json, serializer.ContentType);
    }

    [Fact]
    public void GetSerializer_Should_ReturnJsonSerializer_When_QueryingRequestType()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddRequestHandler<ProcessPaymentHandler>());

        // assert
        var messageType = runtime.Messages.GetMessageType(typeof(ProcessPayment));
        Assert.NotNull(messageType);

        var serializer = messageType.GetSerializer(MessageContentType.Json);
        Assert.NotNull(serializer);
    }

    [Fact]
    public void GetSerializer_Should_ReturnJsonSerializer_When_QueryingResponseType()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddRequestHandler<GetOrderStatusHandler>());

        // assert
        var responseType = runtime.Messages.GetMessageType(typeof(OrderStatusResponse));
        Assert.NotNull(responseType);

        var serializer = responseType.GetSerializer(MessageContentType.Json);
        Assert.NotNull(serializer);
    }

    [Fact]
    public void IsRegistered_Should_ReturnTrue_When_TypeIsKnown()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        Assert.True(runtime.Messages.IsRegistered(typeof(OrderCreated)));
    }

    [Fact]
    public void IsRegistered_Should_ReturnFalse_When_TypeIsUnknown()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        Assert.False(runtime.Messages.IsRegistered(typeof(UnregisteredEvent)));
    }

    [Fact]
    public void GetMessageType_Should_ReturnNull_When_TypeIsUnknown()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        Assert.Null(runtime.Messages.GetMessageType(typeof(UnregisteredEvent)));
    }

    [Fact]
    public void GetMessageTypeByIdentity_Should_ReturnNull_When_IdentityIsUnknown()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        Assert.Null(runtime.Messages.GetMessageType("urn:message:nonexistent:type"));
    }

    [Fact]
    public void MessageTypes_Should_ContainRegisteredTypes_When_RuntimeIsConfigured()
    {
        // arrange & act
        var runtime = CreateRuntime(b =>
        {
            b.AddEventHandler<OrderCreatedHandler>();
            b.AddRequestHandler<ProcessPaymentHandler>();
        });

        // assert
        var runtimeTypes = runtime.Messages.MessageTypes.Select(mt => mt.RuntimeType).ToHashSet();

        Assert.Contains(typeof(OrderCreated), runtimeTypes);
        Assert.Contains(typeof(ProcessPayment), runtimeTypes);
    }

    [Fact]
    public void JsonMessageContentType_Should_HaveCorrectValue_When_Accessed()
    {
        // assert
        Assert.Equal("application/json", MessageContentType.Json.Value);
    }

    [Fact]
    public void Parse_Should_ReturnSingletonJson_When_ParsingJson()
    {
        // act
        var parsed = MessageContentType.Parse("application/json");

        // assert
        Assert.Same(MessageContentType.Json, parsed);
    }

    [Fact]
    public void Parse_Should_ReturnSingletonXml_When_ParsingXml()
    {
        // act
        var parsed = MessageContentType.Parse("application/xml");

        // assert
        Assert.Same(MessageContentType.Xml, parsed);
    }

    [Fact]
    public void Parse_Should_ReturnSingletonProtobuf_When_ParsingProtobuf()
    {
        // act
        var parsed = MessageContentType.Parse("application/protobuf");

        // assert
        Assert.Same(MessageContentType.Protobuf, parsed);
    }

    [Fact]
    public void Parse_Should_ReturnNewInstance_When_ParsingCustomContentType()
    {
        // act
        var parsed = MessageContentType.Parse("application/msgpack");

        // assert
        Assert.NotNull(parsed);
        Assert.Equal("application/msgpack", parsed!.Value);
    }

    [Fact]
    public void Parse_Should_ReturnNull_When_InputIsNullOrEmpty()
    {
        // assert
        Assert.Null(MessageContentType.Parse(null));
        Assert.Null(MessageContentType.Parse(""));
    }

    public sealed class OrderCreated
    {
        public string OrderId { get; init; } = "";
    }

    public sealed class ProcessPayment
    {
        public decimal Amount { get; init; }
    }

    public sealed class GetOrderStatus : IEventRequest<OrderStatusResponse>
    {
        public string OrderId { get; init; } = "";
    }

    public sealed class OrderStatusResponse
    {
        public string Status { get; init; } = "";
    }

    public sealed class UnregisteredEvent
    {
        public string Data { get; init; } = "";
    }

    public sealed class OrderCreatedHandler : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken) => default;
    }

    public sealed class ProcessPaymentHandler : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken) => default;
    }

    public sealed class GetOrderStatusHandler : IEventRequestHandler<GetOrderStatus, OrderStatusResponse>
    {
        public ValueTask<OrderStatusResponse> HandleAsync(GetOrderStatus request, CancellationToken cancellationToken)
        {
            return new(new OrderStatusResponse { Status = "Shipped" });
        }
    }

    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        return (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
    }
}
