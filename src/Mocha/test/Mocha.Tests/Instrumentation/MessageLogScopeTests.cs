using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.Instrumentation;

public sealed class MessageLogScopeTests
{
    private const string OrderPlacedType =
        "urn:message:mocha.tests.instrumentation:message-log-scope-tests.order-placed";
    private const string OrderShippedType =
        "urn:message:mocha.tests.instrumentation:message-log-scope-tests.order-shipped";
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private static readonly Guid s_instanceId = new("00000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Receive_Should_LogHandlerEntriesInMessageScope_When_HandlerLogs()
    {
        // arrange
        var logs = new ScopeRecordingLoggerProvider(static (_, eventId) => eventId.Name == "HandlingOrder");
        var received = new ConcurrentDictionary<string, ReceivedMessage>();
        await using var provider = await CreateBusAsync(
            logs,
            received,
            builder => builder.AddEventHandler<LoggingHandler>(),
            transport => transport.Endpoint("orders").Handler<LoggingHandler>());

        // act
        await PublishAsync(provider);
        var scopes = await logs.Scopes.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal([ExpectedScope("orders", OrderPlacedType, received["orders"])], scopes);
    }

    [Fact]
    public async Task Receive_Should_LogFaultInSingleMessageScope_When_HandlerThrows()
    {
        // arrange
        var logs = new ScopeRecordingLoggerProvider(static (_, eventId) => eventId.Name == "MessageFaulted");
        var received = new ConcurrentDictionary<string, ReceivedMessage>();
        await using var provider = await CreateBusAsync(
            logs,
            received,
            builder => builder.AddEventHandler<ThrowingHandler>(),
            transport => transport.Endpoint("orders").Handler<ThrowingHandler>());

        // act
        await PublishAsync(provider);
        var scopes = await logs.Scopes.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal([ExpectedScope("orders", OrderPlacedType, received["orders"])], scopes);
    }

    [Fact]
    public async Task Receive_Should_LogHandlerEntriesInOwnMessageScope_When_MessageIsPublishedFromHandler()
    {
        // arrange
        var logs = new ScopeRecordingLoggerProvider(static (_, eventId) => eventId.Name == "HandlingShipment");
        var received = new ConcurrentDictionary<string, ReceivedMessage>();
        await using var provider = await CreateBusAsync(
            logs,
            received,
            builder =>
            {
                builder.AddEventHandler<PublishingHandler>();
                builder.AddEventHandler<ShipmentLoggingHandler>();
            },
            transport =>
            {
                transport.Endpoint("orders").Handler<PublishingHandler>();
                transport.Endpoint("shipments").Handler<ShipmentLoggingHandler>();
            });

        // act
        await PublishAsync(provider);
        var scopes = await logs.Scopes.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal([ExpectedScope("shipments", OrderShippedType, received["shipments"])], scopes);
    }

    private static string ExpectedScope(string endpoint, string messageType, ReceivedMessage message)
        => $"InstanceId:{s_instanceId} Transport:memory Endpoint:{endpoint} "
            + $"SourceAddress:memory://mocha.tests/q/{endpoint} "
            + $"MessageId:{message.MessageId} MessageType:{messageType} "
            + $"CorrelationId:{message.CorrelationId} ConversationId:{message.ConversationId} "
            + $"CausationId:{message.CausationId} DeliveryCount:0";

    private static async Task<ServiceProvider> CreateBusAsync(
        ScopeRecordingLoggerProvider logs,
        ConcurrentDictionary<string, ReceivedMessage> received,
        Action<IMessageBusHostBuilder> configure,
        Action<IInMemoryMessagingTransportDescriptor> configureTransport)
    {
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddProvider(logs));
        var builder = services.AddMessageBus();
        builder.Host(host => host.InstanceId(s_instanceId));
        configure(builder);
        builder.ConfigureMessageBus(
            bus => bus.UseReceive(CaptureMiddleware.Create(received), after: "ReceiveInstrumentation"));
        builder.AddInMemory(configureTransport);

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(TestContext.Current.CancellationToken);
        return provider;
    }

    private static async Task PublishAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.PublishAsync(new OrderPlaced(), TestContext.Current.CancellationToken);
    }

    public sealed class OrderPlaced;

    public sealed class OrderShipped;

    public sealed class LoggingHandler(ILogger<LoggingHandler> logger) : IEventHandler<OrderPlaced>
    {
        public ValueTask HandleAsync(OrderPlaced message, CancellationToken cancellationToken)
        {
            logger.LogInformation(new EventId(1, "HandlingOrder"), "Handling order");
            return ValueTask.CompletedTask;
        }
    }

    public sealed class ThrowingHandler : IEventHandler<OrderPlaced>
    {
        public ValueTask HandleAsync(OrderPlaced message, CancellationToken cancellationToken)
            => throw new InvalidOperationException("The handler failed.");
    }

    public sealed class PublishingHandler(IMessageBus bus) : IEventHandler<OrderPlaced>
    {
        public async ValueTask HandleAsync(OrderPlaced message, CancellationToken cancellationToken)
            => await bus.PublishAsync(new OrderShipped(), cancellationToken);
    }

    public sealed class ShipmentLoggingHandler(ILogger<ShipmentLoggingHandler> logger)
        : IEventHandler<OrderShipped>
    {
        public ValueTask HandleAsync(OrderShipped message, CancellationToken cancellationToken)
        {
            logger.LogInformation(new EventId(2, "HandlingShipment"), "Handling shipment");
            return ValueTask.CompletedTask;
        }
    }

    private sealed record ReceivedMessage(
        string? MessageId,
        string? CorrelationId,
        string? ConversationId,
        string? CausationId);

    /// <summary>
    /// Records the generated identifiers of the first message each receive endpoint receives.
    /// </summary>
    private static class CaptureMiddleware
    {
        public static ReceiveMiddlewareConfiguration Create(ConcurrentDictionary<string, ReceivedMessage> received)
            => new(
                (_, next) => context =>
                {
                    received.TryAdd(
                        context.Endpoint.Name,
                        new ReceivedMessage(
                            context.MessageId,
                            context.CorrelationId,
                            context.ConversationId,
                            context.CausationId));
                    return next(context);
                },
                "Capture");
    }

    /// <summary>
    /// Records the text of each scope of the first log entry that matches the filter.
    /// </summary>
    private sealed class ScopeRecordingLoggerProvider(Func<string, EventId, bool> filter)
        : ILoggerProvider, ISupportExternalScope
    {
        private readonly TaskCompletionSource<IReadOnlyList<string>> _scopes =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private IExternalScopeProvider? _scopeProvider;

        public Task<IReadOnlyList<string>> Scopes => _scopes.Task;

        public ILogger CreateLogger(string categoryName) => new RecordingLogger(this, categoryName);

        public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopeProvider = scopeProvider;

        public void Dispose()
        {
        }

        private void Record(string category, EventId eventId)
        {
            if (_scopes.Task.IsCompleted || !filter(category, eventId))
            {
                return;
            }

            var scopes = new List<string>();
            _scopeProvider?.ForEachScope(
                static (scope, list) => list.Add(scope?.ToString() ?? string.Empty),
                scopes);
            _scopes.TrySetResult(scopes);
        }

        private sealed class RecordingLogger(ScopeRecordingLoggerProvider provider, string category) : ILogger
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
                => provider.Record(category, eventId);
        }
    }
}
