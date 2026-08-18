using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests.Behaviors;

/// <summary>
/// Covers the receive endpoint start/stop lifecycle: disposal idempotency and the processor
/// option values the endpoint resolves at startup.
/// </summary>
[Collection("AzureServiceBus")]
public class ReceiveLifecycleTests
{
    private readonly AzureServiceBusFixture _fixture;

    public ReceiveLifecycleTests(AzureServiceBusFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task StopAsync_Should_BeIdempotentAndAllowRestart_When_CalledOnReplyEndpoint()
    {
        // arrange - the reply receive endpoint runs both a processor and a queue heartbeat, so
        // stopping it exercises the disposal of both resources together.
        await using var ctx = _fixture.CreateTestContext();
        await using var bus = await new ServiceCollection()
            .AddMessageBus()
            .AddRequestHandler<GetOrderStatusHandler>()
            .AddAzureServiceBus(ctx)
            .BuildTestBusAsync();

        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        var replyEndpoint = transport.ReplyReceiveEndpoint
            ?? throw new InvalidOperationException("Expected a reply receive endpoint to be configured.");
        Assert.True(replyEndpoint.IsStarted);

        // act - stop twice in a row; the second call must be a safe no-op
        await replyEndpoint.StopAsync(runtime, Xunit.TestContext.Current.CancellationToken);
        await replyEndpoint.StopAsync(runtime, Xunit.TestContext.Current.CancellationToken);

        // assert
        Assert.False(replyEndpoint.IsStarted);

        // act - restarting proves the processor and heartbeat from the stopped endpoint were
        // fully released rather than left bound to disposed links
        await replyEndpoint.StartAsync(runtime, Xunit.TestContext.Current.CancellationToken);

        // assert
        Assert.True(replyEndpoint.IsStarted);
    }

    [Fact]
    public async Task OnStartAsync_Should_ResolveExplicitPrefetchAndConcurrency_When_Configured()
    {
        // arrange
        var logger = new CapturingLogger();
        await using var ctx = _fixture.CreateTestContext();
        var queueName = ctx.QueueName("explicit-opts");
        var builder = new ServiceCollection()
            .AddMessageBus()
            .AddConsumer<NoOpConsumer>()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.AdministrationConnectionString(ctx.AdminConnectionString);
                t.Endpoint("explicit-opts-ep")
                    .Consumer<NoOpConsumer>()
                    .Queue(queueName)
                    .MaxConcurrency(3)
                    .PrefetchCount(37);
            });
        builder.Services.AddSingleton<ILogger<AzureServiceBusReceiveEndpoint>>(logger);
        await using var bus = await builder.BuildTestBusAsync();

        // act
        var entry = Assert.Single(logger.Entries, e => e.Message.Contains("receive endpoint"));

        // assert
        Assert.Equal(37, entry.State["PrefetchCount"]);
        Assert.Equal(3, entry.State["MaxConcurrentCalls"]);
    }

    [Fact]
    public async Task OnStartAsync_Should_ResolveDefaultPrefetchAndConcurrency_When_NotConfigured()
    {
        // arrange
        var logger = new CapturingLogger();
        await using var ctx = _fixture.CreateTestContext();
        var queueName = ctx.QueueName("default-opts");
        var builder = new ServiceCollection()
            .AddMessageBus()
            .AddConsumer<NoOpConsumer>()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.AdministrationConnectionString(ctx.AdminConnectionString);
                t.Endpoint("default-opts-ep").Consumer<NoOpConsumer>().Queue(queueName);
            });
        builder.Services.AddSingleton<ILogger<AzureServiceBusReceiveEndpoint>>(logger);
        await using var bus = await builder.BuildTestBusAsync();

        var expectedMaxConcurrency = Math.Clamp(Environment.ProcessorCount * 2, 1, 1000);

        // act
        var entry = Assert.Single(logger.Entries, e => e.Message.Contains("receive endpoint"));

        // assert
        Assert.Equal(expectedMaxConcurrency, entry.State["MaxConcurrentCalls"]);
        Assert.Equal(expectedMaxConcurrency * 2, entry.State["PrefetchCount"]);
    }

    [Fact]
    public async Task OnStartAsync_Should_ResolveExplicitSessionOptions_When_Configured()
    {
        // arrange
        var logger = new CapturingLogger();
        await using var ctx = _fixture.CreateTestContext();
        var queueName = ctx.QueueName("explicit-session-opts");
        var builder = new ServiceCollection()
            .AddMessageBus()
            .AddConsumer<NoOpConsumer>()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.AdministrationConnectionString(ctx.AdminConnectionString);
                t.DeclareQueue(queueName).RequiresSession();
                t.Endpoint("explicit-session-opts-ep")
                    .Consumer<NoOpConsumer>()
                    .Queue(queueName)
                    .MaxConcurrentSessions(5)
                    .MaxConcurrentCallsPerSession(2)
                    .PrefetchCount(41);
            });
        builder.Services.AddSingleton<ILogger<AzureServiceBusReceiveEndpoint>>(logger);
        await using var bus = await builder.BuildTestBusAsync();

        // act
        var entry = Assert.Single(logger.Entries, e => e.Message.Contains("session receive endpoint"));

        // assert
        Assert.Equal(41, entry.State["PrefetchCount"]);
        Assert.Equal(5, entry.State["MaxConcurrentSessions"]);
        Assert.Equal(2, entry.State["MaxConcurrentCallsPerSession"]);
    }

    [Fact]
    public async Task OnStartAsync_Should_ResolveDefaultSessionOptions_When_NotConfigured()
    {
        // arrange - default MaxConcurrentSessions falls back to MaxConcurrency, and
        // MaxConcurrentCallsPerSession defaults to 1 to preserve in-session ordering.
        var logger = new CapturingLogger();
        await using var ctx = _fixture.CreateTestContext();
        var queueName = ctx.QueueName("default-session-opts");
        var builder = new ServiceCollection()
            .AddMessageBus()
            .AddConsumer<NoOpConsumer>()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.AdministrationConnectionString(ctx.AdminConnectionString);
                t.DeclareQueue(queueName).RequiresSession();
                t.Endpoint("default-session-opts-ep")
                    .Consumer<NoOpConsumer>()
                    .Queue(queueName)
                    .MaxConcurrency(6);
            });
        builder.Services.AddSingleton<ILogger<AzureServiceBusReceiveEndpoint>>(logger);
        await using var bus = await builder.BuildTestBusAsync();

        // act
        var entry = Assert.Single(logger.Entries, e => e.Message.Contains("session receive endpoint"));

        // assert
        Assert.Equal(6, entry.State["MaxConcurrentSessions"]);
        Assert.Equal(1, entry.State["MaxConcurrentCallsPerSession"]);
        Assert.Equal(12, entry.State["PrefetchCount"]);
    }

    public sealed class NoOpConsumer : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context) => default;
    }

    /// <summary>
    /// A hand-written <see cref="ILogger{TCategoryName}"/> that captures structured log entries
    /// for inspection, keyed by the named state values the endpoint logs at startup.
    /// </summary>
    private sealed class CapturingLogger : ILogger<AzureServiceBusReceiveEndpoint>
    {
        public List<CapturedEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NoOpDisposable.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var structuredState = state as IReadOnlyList<KeyValuePair<string, object?>> ?? [];
            var values = structuredState.Where(kv => kv.Value is not null)
                .ToDictionary(kv => kv.Key, kv => kv.Value!);

            Entries.Add(new CapturedEntry(formatter(state, exception), values));
        }

        private sealed class NoOpDisposable : IDisposable
        {
            public static NoOpDisposable Instance { get; } = new();

            public void Dispose() { }
        }
    }

    private sealed record CapturedEntry(string Message, IReadOnlyDictionary<string, object> State);
}
