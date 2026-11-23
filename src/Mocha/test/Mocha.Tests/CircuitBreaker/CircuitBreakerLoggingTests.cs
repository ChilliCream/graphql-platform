using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Mocha.Transport.InMemory;

namespace Mocha.Tests;

public sealed class CircuitBreakerLoggingTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private static readonly Guid s_instanceId = new("00000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task TransportCircuitBreaker_Should_LogTransportCircuitUnderOwnCategory_When_CircuitOpens()
    {
        // arrange
        using var logs = new FakeLoggerProvider();
        await using var provider = await CreateBusAsync(
            logs,
            builder =>
            {
                builder.AddEventHandler<NoOpHandler>();
                // the failure is raised inside the transport circuit breaker and outside the fault handling
                builder.ConfigureMessageBus(
                    bus => bus.UseReceive(ThrowingMiddleware.Create(), after: "TransportCircuitBreaker"));
            },
            transport =>
            {
                transport.ModifyOptions(
                    options => options.CircuitBreaker = new TransportCircuitBreakerOptions
                    {
                        FailureRatio = 0.5,
                        MinimumThroughput = 2,
                        SamplingDuration = TimeSpan.FromSeconds(30),
                        BreakDuration = TimeSpan.FromSeconds(30)
                    });
                transport.Endpoint("orders").Handler<NoOpHandler>();
            });

        // act
        await PublishAsync(provider, count: 2);
        var entry = await logs.Collector.WaitForLogAsync(
            record => record.Id.Name == "CircuitOpen",
            s_timeout,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("Mocha.TransportCircuitBreakerMiddleware", entry.Category);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal(
            "Transport circuit for endpoint orders is open; recovery will be tested after 00:00:30",
            entry.Message);
        Assert.Equal(
            ["InstanceId:00000000-0000-0000-0000-000000000001 Transport:memory Endpoint:orders Circuit:Transport"],
            entry.Scopes.Select(scope => scope?.ToString() ?? string.Empty).ToArray());
    }

    [Fact]
    public async Task ReceiveCircuitBreaker_Should_LogConsumerCircuitUnderOwnCategory_When_CircuitOpens()
    {
        // arrange
        using var logs = new FakeLoggerProvider();
        await using var provider = await CreateBusAsync(
            logs,
            builder =>
            {
                builder.AddEventHandler<ThrowingHandler>();
                builder.AddCircuitBreaker(
                    options =>
                    {
                        options.FailureRatio = 0.5;
                        options.MinimumThroughput = 2;
                        options.SamplingDuration = TimeSpan.FromSeconds(30);
                        options.BreakDuration = TimeSpan.FromSeconds(30);
                    });
            },
            transport => transport.Endpoint("orders").Handler<ThrowingHandler>());

        // act
        await PublishAsync(provider, count: 2);
        var entry = await logs.Collector.WaitForLogAsync(
            record => record.Id.Name == "CircuitOpen",
            s_timeout,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("Mocha.ReceiveCircuitBreakerMiddleware", entry.Category);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal(
            "Consumer circuit for endpoint orders is open; recovery will be tested after 00:00:30",
            entry.Message);
        Assert.Equal(
            ["InstanceId:00000000-0000-0000-0000-000000000001 Transport:memory Endpoint:orders Circuit:Consumer"],
            entry.Scopes.Select(scope => scope?.ToString() ?? string.Empty).ToArray());
    }

    private static async Task<ServiceProvider> CreateBusAsync(
        FakeLoggerProvider logs,
        Action<IMessageBusHostBuilder> configure,
        Action<IInMemoryMessagingTransportDescriptor> configureTransport)
    {
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddProvider(logs));
        var builder = services.AddMessageBus();
        builder.Host(host => host.InstanceId(s_instanceId));
        configure(builder);
        builder.AddInMemory(configureTransport);

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(TestContext.Current.CancellationToken);
        return provider;
    }

    private static async Task PublishAsync(IServiceProvider provider, int count)
    {
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        for (var i = 0; i < count; i++)
        {
            await bus.PublishAsync(new OrderPlaced(), TestContext.Current.CancellationToken);
        }
    }

    public sealed class OrderPlaced;

    public sealed class NoOpHandler : IEventHandler<OrderPlaced>
    {
        public ValueTask HandleAsync(OrderPlaced message, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    public sealed class ThrowingHandler : IEventHandler<OrderPlaced>
    {
        public ValueTask HandleAsync(OrderPlaced message, CancellationToken cancellationToken)
            => throw new InvalidOperationException("The handler failed.");
    }

    private static class ThrowingMiddleware
    {
        public static ReceiveMiddlewareConfiguration Create()
            => new(
                static (_, _) => static _ => throw new InvalidOperationException("The transport failed."),
                "Throwing");
    }
}
