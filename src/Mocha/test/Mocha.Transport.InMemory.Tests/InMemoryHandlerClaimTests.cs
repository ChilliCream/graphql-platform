using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests;

public class InMemoryHandlerClaimTests
{
    [Fact]
    public void Handler_Should_CreateEndpoint_When_Called()
    {
        // arrange & act
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.Handler<OrderCreatedHandler>();
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints
            .OfType<InMemoryReceiveEndpoint>()
            .SingleOrDefault(e => e.Name == "order-created");

        Assert.NotNull(endpoint);
        Assert.Contains(typeof(OrderCreatedHandler), endpoint.Configuration.ConsumerIdentities);
    }

    [Fact]
    public void Handler_Should_ApplyConfig_When_ConfigureEndpointCalled()
    {
        // arrange & act
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.Handler<OrderCreatedHandler>()
                    .ConfigureEndpoint(e => e.MaxConcurrency(5));
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints
            .OfType<InMemoryReceiveEndpoint>()
            .Single(e => e.Name == "order-created");

        Assert.Equal(5, endpoint.Configuration.MaxConcurrency);
    }

    [Fact]
    public void Consumer_Should_CreateEndpoint_When_Called()
    {
        // arrange & act
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddConsumer<TestOrderConsumer>()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.Consumer<TestOrderConsumer>();
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints
            .OfType<InMemoryReceiveEndpoint>()
            .SingleOrDefault(e => e.Name == "test-order");

        Assert.NotNull(endpoint);
        Assert.Contains(typeof(TestOrderConsumer), endpoint.Configuration.ConsumerIdentities);
    }

    [Fact]
    public void Handler_Should_MergeWithExisting_When_ConventionNameMatchesExplicitEndpoint()
    {
        // arrange & act
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.Endpoint("order-created").MaxConcurrency(10);
                t.Handler<OrderCreatedHandler>();
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // assert - only one endpoint with that name, containing both the handler and the concurrency setting
        var endpoints = transport.ReceiveEndpoints
            .OfType<InMemoryReceiveEndpoint>()
            .Where(e => e.Name == "order-created")
            .ToList();

        var endpoint = Assert.Single(endpoints);
        Assert.Contains(typeof(OrderCreatedHandler), endpoint.Configuration.ConsumerIdentities);
        Assert.Equal(10, endpoint.Configuration.MaxConcurrency);
    }

    [Fact]
    public void Handler_Should_CreateSeparateEndpoints_When_MultipleHandlersClaimed()
    {
        // arrange & act
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddEventHandler<OrderCreatedHandler2>()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.Handler<OrderCreatedHandler>();
                t.Handler<OrderCreatedHandler2>();
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // assert
        var endpointNames = transport.ReceiveEndpoints
            .OfType<InMemoryReceiveEndpoint>()
            .Where(e => e.Name == "order-created" || e.Name == "order-created-handler-2")
            .Select(e => e.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.Equal(2, endpointNames.Count);
        Assert.Contains("order-created", endpointNames);
        Assert.Contains("order-created-handler-2", endpointNames);
    }

    public sealed class TestOrderConsumer : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context) => default;
    }
}
