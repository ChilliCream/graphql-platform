using System.Buffers;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Events;
using Mocha.Transport.InMemory;

namespace Mocha.Tests;

public class AotMessageTypeTests
{
    [Fact]
    public void Complete_Should_ResolveFrameworkIdentityViaRuntimeNaming_When_AotConfigurationIncludesFrameworkBaseType()
    {
        // arrange
        var runtime = CreateAotRuntime(builder => builder.ConfigureMessageBus(bus =>
        {
            bus.AddMessageConfiguration(new MessagingMessageConfiguration
            {
                MessageType = typeof(GetOrderStatus),
                Serializer = new StubMessageSerializer(),
                EnclosedTypes = [typeof(GetOrderStatus), typeof(IEventRequest<OrderStatusResponse>)]
            });

            bus.AddMessageConfiguration(new MessagingMessageConfiguration
            {
                MessageType = typeof(OrderStatusResponse),
                Serializer = new StubMessageSerializer(),
                EnclosedTypes = [typeof(OrderStatusResponse)]
            });
        }));

        // act
        var messageType = runtime.Messages.GetMessageType(typeof(GetOrderStatus))!;
        var expected = runtime.Naming.GetMessageIdentity(typeof(IEventRequest<OrderStatusResponse>));

        // assert
        Assert.True(messageType.IsCompleted);
        Assert.Contains(expected, messageType.EnclosedMessageIdentities);
    }

    [Fact]
    public void Complete_Should_UseCustomNamingConvention_When_FrameworkBaseTypeResolvedViaRuntimeNaming()
    {
        // arrange
        var runtime = CreateAotRuntime(
            builder =>
            {
                // Registered on the outer service provider because MessageBusBuilder.AddCoreServices
                // resolves IBusNamingConventions from application services.
                builder.Services.AddSingleton<IBusNamingConventions, PrefixingNamingConventions>();

                builder.ConfigureMessageBus(bus =>
                {
                    bus.AddMessageConfiguration(new MessagingMessageConfiguration
                    {
                        MessageType = typeof(GetOrderStatus),
                        Serializer = new StubMessageSerializer(),
                        EnclosedTypes = [typeof(GetOrderStatus), typeof(IEventRequest<OrderStatusResponse>)]
                    });

                    bus.AddMessageConfiguration(new MessagingMessageConfiguration
                    {
                        MessageType = typeof(OrderStatusResponse),
                        Serializer = new StubMessageSerializer(),
                        EnclosedTypes = [typeof(OrderStatusResponse)]
                    });
                });
            });

        // act
        var messageType = runtime.Messages.GetMessageType(typeof(GetOrderStatus))!;
        var defaultIdentity = new DefaultNamingConventions(runtime.Host)
            .GetMessageIdentity(typeof(IEventRequest<OrderStatusResponse>));

        // assert
        Assert.True(messageType.IsCompleted);
        Assert.Contains(
            $"custom:{typeof(IEventRequest<OrderStatusResponse>).FullName}",
            messageType.EnclosedMessageIdentities);
        Assert.DoesNotContain(defaultIdentity, messageType.EnclosedMessageIdentities);
    }

    [Fact]
    public void Complete_Should_Throw_When_AotModeWithoutEnclosedTypes()
    {
        // act & assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Contains("No enclosed types provided", exception.Message);

        static void Act()
        {
            CreateAotRuntime(
                builder => builder.ConfigureMessageBus(bus => bus.AddMessage<GetOrderStatus>(static _ => { })));
        }
    }

    public sealed class GetOrderStatus : IEventRequest<OrderStatusResponse>
    {
        public string OrderId { get; init; } = "";
    }

    public sealed class OrderStatusResponse
    {
        public string Status { get; init; } = "";
    }

    private sealed class StubMessageSerializer : IMessageSerializer
    {
        public MessageContentType ContentType => MessageContentType.Json;

        public T? Deserialize<T>(ReadOnlyMemory<byte> body) => default;

        public object? Deserialize(ReadOnlyMemory<byte> body) => null;

        public void Serialize<T>(T message, IBufferWriter<byte> writer)
        {
        }

        public void Serialize(object message, IBufferWriter<byte> writer)
        {
        }
    }

    /// <summary>
    /// Test double that prefixes every identity with "custom:" so we can observe whether
    /// the runtime resolves framework base type identities through the configured convention
    /// instead of a compile-time-baked URN.
    /// </summary>
    private sealed class PrefixingNamingConventions : IBusNamingConventions
    {
        public string GetReceiveEndpointName(InboundRoute route, ReceiveEndpointKind kind) => route.Consumer?.Name ?? "unknown";

        public string GetReceiveEndpointName(Type handlerType, ReceiveEndpointKind kind) => handlerType.Name;

        public string GetReceiveEndpointName(string name, ReceiveEndpointKind kind) => name;

        public string GetSagaName(Type sagaType) => sagaType.Name;

        public string GetInstanceEndpoint(Guid instanceId) => $"response-{instanceId:N}";

        public string GetSendEndpointName(Type messageType) => messageType.Name;

        public string GetPublishEndpointName(Type messageType) => messageType.Name;

        public string GetMessageIdentity(Type messageType) => $"custom:{messageType.FullName}";
    }

    private static MessagingRuntime CreateAotRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder.ModifyOptions(static o => o.IsAotCompatible = true);

        // AddMessageBus auto-registers the internal acknowledgement events via AddDefaults;
        // in AOT mode they also need enclosed-types configuration to pass the Complete guard.
        RegisterInternalEvents(builder);

        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        return (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
    }

    private static void RegisterInternalEvents(IMessageBusHostBuilder builder)
    {
        builder.ConfigureMessageBus(bus =>
        {
            bus.AddMessage<NotAcknowledgedEvent>(
                d => d.Extend().Configuration.EnclosedTypes = [typeof(NotAcknowledgedEvent)]);
            bus.AddMessage<AcknowledgedEvent>(
                d => d.Extend().Configuration.EnclosedTypes = [typeof(AcknowledgedEvent)]);
        });
    }
}
