using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests.Behaviors;

[Collection("AzureServiceBus")]
public class BusDefaultsIntegrationTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly AzureServiceBusFixture _fixture;

    public BusDefaultsIntegrationTests(AzureServiceBusFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ConfigureDefaults_Should_ApplyPrefetchCount_When_EndpointDoesNotOverride()
    {
        // arrange - bus-level default for PrefetchCount is applied to all endpoints unless overridden
        var ctx = _fixture.CreateTestContext();
        await using var bus = await new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.ConfigureDefaults(d => d.Endpoint.PrefetchCount = 8);
            })
            .BuildTestBusAsync();

        // act - locate the endpoint owning the OrderCreated route
        var endpoint = GetDefaultEndpoint(bus);

        // assert - the bus-level default propagated into the endpoint configuration
        Assert.Equal(8, endpoint.Configuration.PrefetchCount);
    }

    [Fact]
    public async Task ConfigureDefaults_Should_ApplyMaxConcurrency_When_EndpointDoesNotOverride()
    {
        // arrange - bus-level default for MaxConcurrency is applied to all endpoints unless overridden
        var ctx = _fixture.CreateTestContext();
        await using var bus = await new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.ConfigureDefaults(d => d.Endpoint.MaxConcurrency = 3);
            })
            .BuildTestBusAsync();

        // act
        var endpoint = GetDefaultEndpoint(bus);

        // assert
        Assert.Equal(3, endpoint.Configuration.MaxConcurrency);
    }

    [Fact]
    public async Task ConfigureDefaults_Should_NotOverrideExplicitPrefetch_When_EndpointSpecifiesValue()
    {
        // arrange - per-endpoint PrefetchCount must win over bus-level defaults
        var ctx = _fixture.CreateTestContext();
        await using var bus = await new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.ConfigureDefaults(d => d.Endpoint.PrefetchCount = 8);
                t.Endpoint("override-ep").Handler<OrderCreatedHandler>().PrefetchCount(2);
            })
            .BuildTestBusAsync();

        // act
        var endpoint = GetEndpointByName(bus, "override-ep");

        // assert - the explicit value wins
        Assert.Equal(2, endpoint.Configuration.PrefetchCount);
    }

    [Fact]
    public async Task ConfigureDefaults_Should_NotOverrideExplicitMaxConcurrency_When_EndpointSpecifiesValue()
    {
        // arrange - per-endpoint MaxConcurrency must win over bus-level defaults
        var ctx = _fixture.CreateTestContext();
        await using var bus = await new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.ConfigureDefaults(d => d.Endpoint.MaxConcurrency = 3);
                t.Endpoint("override-ep").Handler<OrderCreatedHandler>().MaxConcurrency(7);
            })
            .BuildTestBusAsync();

        // act
        var endpoint = GetEndpointByName(bus, "override-ep");

        // assert - the explicit value wins
        Assert.Equal(7, endpoint.Configuration.MaxConcurrency);
    }

    [Fact]
    public async Task ConfigureDefaults_Should_DeliverMessages_When_DefaultsApplied()
    {
        // arrange - confirm the configured endpoints still operate end-to-end with defaults
        var recorder = new MessageRecorder();
        var ctx = _fixture.CreateTestContext();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.ConfigureDefaults(d =>
                {
                    d.Endpoint.PrefetchCount = 4;
                    d.Endpoint.MaxConcurrency = 2;
                });
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-DEFAULTS" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the event within timeout");

        var order = Assert.IsType<OrderCreated>(Assert.Single(recorder.Messages));
        Assert.Equal("ORD-DEFAULTS", order.OrderId);
    }

    [Fact]
    public async Task ConfigureDefaults_Should_PropagateToDeclaredQueue_When_QueueOmitsAutoDelete()
    {
        // arrange - a queue declared without AutoDelete should inherit the bus-level default
        var ctx = _fixture.CreateTestContext();
        await using var bus = await new ServiceCollection()
            .AddMessageBus()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.ConfigureDefaults(d => d.Queue.AutoDelete = true);
                t.DeclareQueue("default-applies-q");
            })
            .BuildTestBusAsync();

        // act
        var transport = GetTransport(bus);
        var topology = (AzureServiceBusMessagingTopology)transport.Topology;
        var queue = topology.Queues.First(q => q.Name == "default-applies-q");

        // assert - default AutoDelete propagated because the descriptor did not specify a value
        Assert.True(queue.AutoDelete);
    }

    [Fact]
    public async Task ConfigureDefaults_Should_NotOverrideExplicitQueueSetting_When_QueueDeclaresAutoDelete()
    {
        // arrange - a queue that explicitly sets AutoDelete must keep its own value
        var ctx = _fixture.CreateTestContext();
        await using var bus = await new ServiceCollection()
            .AddMessageBus()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.ConfigureDefaults(d => d.Queue.AutoDelete = true);
                t.DeclareQueue("override-q").AutoDelete(false);
            })
            .BuildTestBusAsync();

        // act
        var transport = GetTransport(bus);
        var topology = (AzureServiceBusMessagingTopology)transport.Topology;
        var queue = topology.Queues.First(q => q.Name == "override-q");

        // assert - the per-queue value wins over the bus-level default
        Assert.False(queue.AutoDelete);
    }

    private static AzureServiceBusReceiveEndpoint GetDefaultEndpoint(TestBus bus)
    {
        var transport = GetTransport(bus);
        return transport.ReceiveEndpoints
            .OfType<AzureServiceBusReceiveEndpoint>()
            .First(e => e.Kind == ReceiveEndpointKind.Default);
    }

    private static AzureServiceBusReceiveEndpoint GetEndpointByName(TestBus bus, string name)
    {
        var transport = GetTransport(bus);
        return transport.ReceiveEndpoints
            .OfType<AzureServiceBusReceiveEndpoint>()
            .First(e => e.Name == name);
    }

    private static AzureServiceBusMessagingTransport GetTransport(TestBus bus)
    {
        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        return runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
    }
}
