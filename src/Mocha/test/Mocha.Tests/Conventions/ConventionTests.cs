using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Tests;

public class ConventionTests
{
    [Fact]
    public void MessageTypeIdentity_Should_FollowURNConvention_When_MessageTypeIsRegistered()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated));
        Assert.NotNull(messageType);
        Assert.StartsWith("urn:message:", messageType.Identity);
        Assert.Contains("order-created", messageType.Identity);
    }

    [Fact]
    public void MessageTypeIdentity_Should_IncludeNamespace_When_MessageTypeIsRegistered()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated));
        Assert.NotNull(messageType);
        // namespace gets converted to kebab-case URN segment
        Assert.Contains("chilli-cream", messageType.Identity);
    }

    [Fact]
    public void ConsumerName_Should_DefaultToHandlerTypeName_When_NoCustomNameIsProvided()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        var consumer = runtime.Consumers.FirstOrDefault(c => c.Name == nameof(OrderCreatedHandler));
        Assert.NotNull(consumer);
    }

    [Fact]
    public void ConsumerName_Should_UseCustomValue_When_CustomNameIsProvided()
    {
        // arrange & act
        var runtime = CreateRuntime(b =>
        {
            b.ConfigureMessageBus(h =>
                ((MessageBusBuilder)h).AddHandler<OrderCreatedHandler>(d => d.Name("MyCustomConsumer"))
            );
            b.Services.AddScoped<OrderCreatedHandler>();
        });

        // assert
        var consumer = runtime.Consumers.FirstOrDefault(c => c.Name == "MyCustomConsumer");
        Assert.NotNull(consumer);
        Assert.Equal(typeof(OrderCreatedHandler), consumer.Identity);
    }

    [Fact]
    public void SubscribeEndpointName_Should_UseKebabCaseHandlerName_When_EventHandlerIsAdded()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(OrderCreatedHandler));
        var route = Assert.Single(runtime.Router.GetInboundByConsumer(consumer));
        Assert.NotNull(route.Endpoint);
        // Handler name "OrderCreatedHandler" -> "order-created" (kebab-case, suffix stripped)
        Assert.Contains("order-created", route.Endpoint.Name);
    }

    [Fact]
    public void SendEndpointName_Should_UseKebabCaseMessageTypeName_When_RequestHandlerIsAdded()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddRequestHandler<ProcessPaymentHandler>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(ProcessPaymentHandler));
        var route = Assert.Single(runtime.Router.GetInboundByConsumer(consumer));
        Assert.NotNull(route.Endpoint);
        // Message type "ProcessPayment" -> "process-payment" (kebab-case)
        Assert.Contains("process-payment", route.Endpoint.Name);
    }

    [Fact]
    public void Runtime_Should_HaveConventionRegistry_When_CreatedWithHandler()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        Assert.NotNull(runtime.Conventions);
    }

    [Fact]
    public void ConventionRegistry_Should_ContainMessageTypePostConfigureConvention_When_RuntimeIsCreated()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert - the MessageTypePostConfigureConvention is always added
        var conventions = runtime.Conventions.GetConventions<IMessageTypeConfigurationConvention>();
        Assert.NotEmpty(conventions);
    }

    [Fact]
    public void Runtime_Should_HaveNamingConventions_When_CreatedWithHandler()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        Assert.NotNull(runtime.Naming);
    }

    // =========================================================================
    // Test Types
    // =========================================================================

    public sealed class OrderCreated
    {
        public string OrderId { get; init; } = "";
    }

    public sealed class ProcessPayment
    {
        public decimal Amount { get; init; }
    }

    public sealed class OrderCreatedHandler : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken) => default;
    }

    public sealed class ProcessPaymentHandler : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken) => default;
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
