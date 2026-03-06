using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mocha.Features;
using Mocha.Middlewares;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.Middlewares.Receive;

/// <summary>
/// Base class for testing receive middlewares. Provides helpers to create middleware
/// contexts, invoke middleware pipelines, and verify behavior.
/// </summary>
public abstract class ReceiveMiddlewareTestBase
{
    protected static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Creates a minimal service provider with common services.
    /// </summary>
    protected static IServiceProvider CreateServices(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Creates a test message envelope.
    /// </summary>
    protected static MessageEnvelope CreateEnvelope(
        string? messageId = null,
        string? correlationId = null,
        string? conversationId = null,
        string? messageType = null,
        string? responseAddress = null,
        DateTimeOffset? sentAt = null,
        DateTimeOffset? deliverBy = null,
        int? deliveryCount = null,
        ImmutableArray<string>? enclosedMessageTypes = null)
    {
        return new MessageEnvelope
        {
            MessageId = messageId ?? Guid.NewGuid().ToString(),
            CorrelationId = correlationId,
            ConversationId = conversationId,
            MessageType = messageType,
            ResponseAddress = responseAddress,
            SentAt = sentAt ?? DateTimeOffset.UtcNow,
            DeliverBy = deliverBy,
            DeliveryCount = deliveryCount ?? 1,
            Body = Array.Empty<byte>(),
            EnclosedMessageTypes = enclosedMessageTypes
        };
    }

    /// <summary>
    /// Creates a delegate that tracks invocations.
    /// </summary>
    protected static ReceiveDelegate CreateTrackingDelegate(InvocationTracker tracker)
    {
        return ctx =>
        {
            tracker.Invoke(ctx);
            return ValueTask.CompletedTask;
        };
    }

    /// <summary>
    /// Creates a delegate that throws an exception.
    /// </summary>
    protected static ReceiveDelegate CreateThrowingDelegate(Exception exception)
    {
        return _ => throw exception;
    }

    /// <summary>
    /// Creates a delegate that marks the message as consumed.
    /// </summary>
    protected static ReceiveDelegate CreateConsumingDelegate()
    {
        return ctx =>
        {
            var feature = ctx.Features.GetOrSet<ReceiveConsumerFeature>();
            feature.MessageConsumed = true;
            return ValueTask.CompletedTask;
        };
    }

    /// <summary>
    /// Creates a delegate that does nothing (passthrough).
    /// </summary>
    protected static ReceiveDelegate CreatePassthroughDelegate()
    {
        return _ => ValueTask.CompletedTask;
    }

    /// <summary>
    /// Creates a delegate that introduces a delay.
    /// </summary>
    protected static ReceiveDelegate CreateDelayedDelegate(TimeSpan delay)
    {
        return async ctx =>
            await Task.Delay(delay, ctx.CancellationToken);
    }

    /// <summary>
    /// Creates a full messaging bus for integration-style tests.
    /// </summary>
    protected static async Task<ServiceProvider> CreateBusAsync(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return provider;
    }

    /// <summary>
    /// Helper class to track delegate invocations.
    /// </summary>
    protected sealed class InvocationTracker
    {
        private readonly ConcurrentBag<IReceiveContext> _invocations = [];
        private readonly SemaphoreSlim _semaphore = new(0);

        public IReadOnlyCollection<IReceiveContext> Invocations => _invocations;
        public int Count => _invocations.Count;
        public bool WasInvoked => !_invocations.IsEmpty;

        public void Invoke(IReceiveContext context)
        {
            _invocations.Add(context);
            _semaphore.Release();
        }

        public async Task<bool> WaitAsync(TimeSpan timeout, int expectedCount = 1)
        {
            for (var i = 0; i < expectedCount; i++)
            {
                if (!await _semaphore.WaitAsync(timeout))
                {
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// Test event for integration tests.
    /// </summary>
    protected sealed class TestEvent
    {
        public required string Id { get; init; }
        public string? Data { get; init; }
    }

    /// <summary>
    /// Test request for integration tests.
    /// </summary>
    protected sealed class TestRequest : IEventRequest<TestResponse>
    {
        public required string Id { get; init; }
    }

    /// <summary>
    /// Test response for integration tests.
    /// </summary>
    protected sealed class TestResponse
    {
        public required string Id { get; init; }
        public required string Result { get; init; }
    }

    /// <summary>
    /// Lightweight stub implementing <see cref="IReceiveContext"/> for unit tests.
    /// All properties are settable with sensible defaults.
    /// </summary>
    protected class StubReceiveContext : IReceiveContext
    {
        public IHeaders Headers { get; } = new Headers();
        IReadOnlyHeaders IMessageContext.Headers => Headers;
        public IFeatureCollection Features { get; } = new FeatureCollection();
        public MessagingTransport Transport { get; set; } = null!;
        public ReceiveEndpoint Endpoint { get; set; } = null!;
        public string? MessageId { get; set; } = Guid.NewGuid().ToString();
        public string? CorrelationId { get; set; }
        public string? ConversationId { get; set; }
        public string? CausationId { get; set; }
        public Uri? SourceAddress { get; set; }
        public Uri? DestinationAddress { get; set; }
        public Uri? ResponseAddress { get; set; }
        public Uri? FaultAddress { get; set; }
        public MessageContentType? ContentType { get; set; }
        public MessageType? MessageType { get; set; }
        public DateTimeOffset? SentAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? DeliverBy { get; set; }
        public int? DeliveryCount { get; set; } = 1;
        public ReadOnlyMemory<byte> Body => Array.Empty<byte>();
        public MessageEnvelope? Envelope { get; set; }
        public IRemoteHostInfo Host { get; set; } = null!;
        public IMessagingRuntime Runtime { get; set; } = null!;
        public CancellationToken CancellationToken { get; set; }
        public IServiceProvider Services { get; set; } = null!;

        public void SetEnvelope(MessageEnvelope envelope) => Envelope = envelope;
    }
}
