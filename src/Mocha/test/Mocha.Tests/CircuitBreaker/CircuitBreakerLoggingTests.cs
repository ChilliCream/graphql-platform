using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Transport.InMemory;

namespace Mocha.Tests;

public sealed class CircuitBreakerLoggingTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task TransportCircuitBreaker_Should_LogTransportCircuitUnderOwnCategory_When_CircuitOpens()
    {
        // arrange
        using var logs = new RecordingLoggerProvider("CircuitOpen");
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
        var entry = await logs.Entry.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            new LogEntry(
                "Mocha.TransportCircuitBreakerMiddleware",
                LogLevel.Warning,
                "Transport circuit for endpoint orders is open; recovery will be tested after 00:00:30"),
            entry);
    }

    [Fact]
    public async Task ReceiveCircuitBreaker_Should_LogConsumerCircuitUnderOwnCategory_When_CircuitOpens()
    {
        // arrange
        using var logs = new RecordingLoggerProvider("CircuitOpen");
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
        var entry = await logs.Entry.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            new LogEntry(
                "Mocha.ReceiveCircuitBreakerMiddleware",
                LogLevel.Warning,
                "Consumer circuit for endpoint orders is open; recovery will be tested after 00:00:30"),
            entry);
    }

    private static async Task<ServiceProvider> CreateBusAsync(
        RecordingLoggerProvider logs,
        Action<IMessageBusHostBuilder> configure,
        Action<IInMemoryMessagingTransportDescriptor> configureTransport)
    {
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddProvider(logs));
        var builder = services.AddMessageBus();
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

    private sealed record LogEntry(string Category, LogLevel Level, string Message);

    /// <summary>
    /// Records the first log entry with the given event name, together with the category of its logger.
    /// </summary>
    private sealed class RecordingLoggerProvider(string eventName) : ILoggerProvider
    {
        private readonly TaskCompletionSource<LogEntry> _entry =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<LogEntry> Entry => _entry.Task;

        private string EventName => eventName;

        public ILogger CreateLogger(string categoryName) => new RecordingLogger(this, categoryName);

        public void Dispose()
        {
        }

        private sealed class RecordingLogger(RecordingLoggerProvider provider, string category) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull
                => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (eventId.Name == provider.EventName)
                {
                    provider._entry.TrySetResult(new LogEntry(category, logLevel, formatter(state, exception)));
                }
            }
        }
    }
}
