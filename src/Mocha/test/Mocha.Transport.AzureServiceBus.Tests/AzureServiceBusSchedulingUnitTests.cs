using System.Collections.Immutable;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;
using Mocha.Scheduling;
using Mocha.Transport.AzureServiceBus.Scheduling;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests;

public sealed class AzureServiceBusSchedulingUnitTests
{
    [Fact]
    public void CreateToken_Should_Roundtrip_When_EntityPathContainsDelimitersAndUnicode()
    {
        var token = AzureServiceBusScheduledMessageStore.CreateToken(
            "owner",
            "orders:priority/über",
            42);

        var success = AzureServiceBusScheduledMessageStore.TryParseToken(
            token,
            "owner",
            out var entityPath,
            out var sequenceNumber);

        Assert.True(success);
        Assert.Equal("orders:priority/über", entityPath);
        Assert.Equal(42, sequenceNumber);
        Assert.StartsWith("asb:v1:owner:", token, StringComparison.Ordinal);
    }

    [Fact]
    public void TryParseToken_Should_ReturnFalse_When_OwnerDoesNotMatch()
    {
        var token = AzureServiceBusScheduledMessageStore.CreateToken("owner-a", "orders", 42);

        var success = AzureServiceBusScheduledMessageStore.TryParseToken(
            token,
            "owner-b",
            out var entityPath,
            out var sequenceNumber);

        Assert.False(success);
        Assert.Equal(string.Empty, entityPath);
        Assert.Equal(0, sequenceNumber);
    }

    [Theory]
    [InlineData("")]
    [InlineData("asb:")]
    [InlineData("asb:orders:42")]
    [InlineData("asb:v2:owner:b3JkZXJz:42")]
    [InlineData("asb:v1:owner:not_base64!:42")]
    [InlineData("asb:v1:owner:b3JkZXJz:0")]
    [InlineData("asb:v1:owner:b3JkZXJz:-1")]
    [InlineData("asb:v1:owner:b3JkZXJz:42:extra")]
    public void TryParseToken_Should_ReturnFalse_When_TokenIsMalformed(string token)
    {
        var success = AzureServiceBusScheduledMessageStore.TryParseToken(
            token,
            "owner",
            out _,
            out _);

        Assert.False(success);
    }

    [Fact]
    public void CreateOwner_Should_BeStable_When_NamespaceCasingAndTrailingDotDiffer()
    {
        var first = AzureServiceBusScheduledMessageStore.CreateOwner(
            "primary",
            "Orders.ServiceBus.Windows.Net.");
        var second = AzureServiceBusScheduledMessageStore.CreateOwner(
            "primary",
            "orders.servicebus.windows.net");

        Assert.Equal(first, second);
    }

    [Fact]
    public void CreateOwner_Should_Differ_When_TransportOrNamespaceDiffers()
    {
        var owner = AzureServiceBusScheduledMessageStore.CreateOwner(
            "primary",
            "orders.servicebus.windows.net");
        var otherTransport = AzureServiceBusScheduledMessageStore.CreateOwner(
            "secondary",
            "orders.servicebus.windows.net");
        var otherNamespace = AzureServiceBusScheduledMessageStore.CreateOwner(
            "primary",
            "billing.servicebus.windows.net");

        Assert.NotEqual(owner, otherTransport);
        Assert.NotEqual(owner, otherNamespace);
        Assert.NotEqual(otherTransport, otherNamespace);
    }

    [Fact]
    public void AddAzureServiceBus_Should_RegisterStoreForEachExactTransportInstance_When_CalledTwice()
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder.AddAzureServiceBus(_ => { });
        builder.AddAzureServiceBus(_ => { });
        using var provider = services.BuildServiceProvider();

        var registrations = provider.GetServices<ScheduledMessageStoreRegistration>().ToArray();

        Assert.Equal(2, registrations.Length);
        Assert.All(
            registrations,
            registration =>
            {
                Assert.IsType<AzureServiceBusMessagingTransport>(registration.Transport);
                Assert.Equal(AzureServiceBusScheduledMessageStore.TokenPrefix, registration.TokenPrefix);
                Assert.False(registration.IsFallback);
            });
        Assert.NotSame(registrations[0].Transport, registrations[1].Transport);
    }

    [Fact]
    public void ResolveStore_Should_Work_When_TransportRuntimeIsFinalized()
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder.AddAzureServiceBus(t => t.ConnectionString(DummyConnectionString));
        builder.BuildRuntime();
        using var provider = services.BuildServiceProvider();
        var registration = provider.GetRequiredService<ScheduledMessageStoreRegistration>();

        var store = registration.Resolve(provider);

        Assert.IsType<AzureServiceBusScheduledMessageStore>(store);
    }

    [Fact]
    public void CreateMessage_Should_MapEnvelopeAndNativeProperties_When_MessageIsScheduled()
    {
        var headers = new Headers();
        headers.Set(AzureServiceBusMessageHeaders.SessionId, "session-1");
        headers.Set(AzureServiceBusMessageHeaders.PartitionKey, "session-1");
        headers.Set(AzureServiceBusMessageHeaders.ReplyToSessionId, "reply-session");
        headers.Set(AzureServiceBusMessageHeaders.To, "forward-target");
        headers.Set("custom", "value");
        var customDate = new DateTimeOffset(2026, 7, 13, 10, 0, 0, TimeSpan.Zero);
        headers.Set("custom-date", customDate);

        var enqueueTime = new DateTimeOffset(2026, 7, 13, 11, 0, 0, TimeSpan.Zero);
        var envelope = new MessageEnvelope
        {
            MessageId = "message-1",
            CorrelationId = "correlation-1",
            ConversationId = "conversation-1",
            CausationId = "causation-1",
            SourceAddress = "azuresb://example/q/source",
            DestinationAddress = "azuresb://example/q/orders",
            ResponseAddress = "azuresb://example/q/replies",
            FaultAddress = "azuresb://example/q/orders_error",
            ContentType = "application/json",
            MessageType = "OrderCreated",
            EnclosedMessageTypes = ImmutableArray.Create("OrderCreated", "IEvent"),
            SentAt = enqueueTime.AddMinutes(-1),
            DeliverBy = enqueueTime.AddMinutes(5),
            Headers = headers,
            Body = "{}"u8.ToArray()
        };

        var message = AzureServiceBusMessageFactory.Create(envelope, enqueueTime);

        Assert.Equal("message-1", message.MessageId);
        Assert.Equal("correlation-1", message.CorrelationId);
        Assert.Equal("application/json", message.ContentType);
        Assert.Equal("OrderCreated", message.Subject);
        Assert.Equal("azuresb://example/q/replies", message.ReplyTo);
        Assert.Equal("session-1", message.SessionId);
        Assert.Equal("session-1", message.PartitionKey);
        Assert.Equal("reply-session", message.ReplyToSessionId);
        Assert.Equal("forward-target", message.To);
        Assert.Equal(TimeSpan.FromMinutes(5), message.TimeToLive);
        Assert.Equal(envelope.Body, message.Body.ToMemory());

        var properties = message.ApplicationProperties;
        Assert.Equal("conversation-1", properties[MessageHeaders.Transport.ConversationId.Key]);
        Assert.Equal("causation-1", properties[MessageHeaders.Transport.CausationId.Key]);
        Assert.Equal("azuresb://example/q/source", properties[MessageHeaders.Transport.SourceAddress.Key]);
        Assert.Equal("azuresb://example/q/orders", properties[MessageHeaders.Transport.DestinationAddress.Key]);
        Assert.Equal("azuresb://example/q/orders_error", properties[MessageHeaders.Transport.FaultAddress.Key]);
        Assert.Equal("OrderCreated", properties[MessageHeaders.Transport.MessageType.Key]);
        Assert.Equal("OrderCreated;IEvent", properties[MessageHeaders.Transport.EnclosedMessageTypes.Key]);
        Assert.Equal(
            enqueueTime.AddMinutes(-1).ToUnixTimeMilliseconds(),
            properties[AzureServiceBusMessageHeaders.SentAt]);
        Assert.Equal("value", properties["custom"]);
        Assert.Equal(customDate, properties["custom-date"]);
        Assert.Equal(10, properties.Count);
    }

    [Fact]
    public void CreateMessage_Should_UseMinimumTimeToLive_When_DeadlineHasPassed()
    {
        var enqueueTime = new DateTimeOffset(2026, 7, 13, 11, 0, 0, TimeSpan.Zero);
        var envelope = new MessageEnvelope
        {
            MessageId = "message-1",
            DeliverBy = enqueueTime.AddSeconds(-1),
            Body = "{}"u8.ToArray()
        };

        var message = AzureServiceBusMessageFactory.Create(envelope, enqueueTime);

        Assert.Equal(TimeSpan.FromMilliseconds(1), message.TimeToLive);
    }

    [Fact]
    public void CreateMessage_Should_DefaultPartitionKeyToSessionId_When_PartitionKeyIsAbsent()
    {
        var headers = new Headers();
        headers.Set(AzureServiceBusMessageHeaders.SessionId, "session-1");
        var envelope = new MessageEnvelope
        {
            MessageId = "message-1",
            Headers = headers,
            Body = "{}"u8.ToArray()
        };

        var message = AzureServiceBusMessageFactory.Create(envelope, DateTimeOffset.UtcNow);

        Assert.Equal("session-1", message.SessionId);
        Assert.Equal("session-1", message.PartitionKey);
    }

    [Fact]
    public void CreateMessage_Should_Throw_When_PartitionKeyDoesNotMatchSessionId()
    {
        var headers = new Headers();
        headers.Set(AzureServiceBusMessageHeaders.SessionId, "session-1");
        headers.Set(AzureServiceBusMessageHeaders.PartitionKey, "partition-2");
        var envelope = new MessageEnvelope
        {
            MessageId = "message-1",
            Headers = headers,
            Body = "{}"u8.ToArray()
        };

        Assert.Throws<InvalidOperationException>(() =>
            AzureServiceBusMessageFactory.Create(envelope, DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(ServiceBusFailureReason.MessageNotFound)]
    [InlineData(ServiceBusFailureReason.MessagingEntityNotFound)]
    public async Task CancelAsync_Should_ReturnFalse_When_ScheduledMessageOrEntityNoLongerExists(
        ServiceBusFailureReason reason)
    {
        // arrange
        var (client, bus) = CreateBus();
        await using var busScope = bus;
        using var scope = busScope.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var result = await messageBus.ScheduleSendAsync(
            new ProcessPayment { OrderId = "ORD-1", Amount = 10m },
            DateTimeOffset.UtcNow.AddMinutes(5),
            CancellationToken.None);
        client.CreatedSender!.CancelFailure = new ServiceBusException("gone", reason);

        // act
        var cancelled = await messageBus.CancelScheduledMessageAsync(result.Token!, CancellationToken.None);

        // assert
        Assert.False(cancelled);
        Assert.Equal(1, client.CreatedSender.CancelScheduledMessageCallCount);
    }

    [Fact]
    public async Task CancelAsync_Should_PropagateException_When_ServiceBusFailureReasonIsUnrelated()
    {
        // arrange
        var (client, bus) = CreateBus();
        await using var busScope = bus;
        using var scope = busScope.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var result = await messageBus.ScheduleSendAsync(
            new ProcessPayment { OrderId = "ORD-1", Amount = 10m },
            DateTimeOffset.UtcNow.AddMinutes(5),
            CancellationToken.None);
        client.CreatedSender!.CancelFailure =
            new ServiceBusException("timeout", ServiceBusFailureReason.ServiceTimeout);

        // act
        var exception = await Assert.ThrowsAsync<ServiceBusException>(
            () => messageBus.CancelScheduledMessageAsync(result.Token!, CancellationToken.None).AsTask());

        // assert
        Assert.Equal(ServiceBusFailureReason.ServiceTimeout, exception.Reason);
        Assert.Equal(1, client.CreatedSender.CancelScheduledMessageCallCount);
    }

    private static (CancelOutcomeServiceBusClient Client, TestBus Bus) CreateBus()
    {
        var client = new CancelOutcomeServiceBusClient();
        var services = new ServiceCollection();
        services.AddSingleton<ServiceBusClient>(client);
        services.AddSingleton<ServiceBusAdministrationClient>(new FakeServiceBusAdministrationClient());
        var builder = services
            .AddMessageBus()
            .AddAzureServiceBus(t => t.DispatchEndpoint("payments").ToQueue("payments").Send<ProcessPayment>());
        var provider = builder.Services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();

        return (client, new TestBus(provider, runtime));
    }

    private const string DummyConnectionString =
        "Endpoint=sb://localhost/;SharedAccessKeyName=test;SharedAccessKey=test";

    /// <summary>
    /// A <see cref="ServiceBusClient"/> test double that hands out a single
    /// <see cref="CancelOutcomeServiceBusSender"/> so tests can control the outcome of
    /// <see cref="ServiceBusSender.CancelScheduledMessageAsync(long, CancellationToken)"/> without a
    /// live namespace. The Azure Service Bus emulator cannot produce the MessageNotFound or
    /// MessagingEntityNotFound cancellation reasons, so this facade is the only way to exercise them.
    /// </summary>
    private sealed class CancelOutcomeServiceBusClient : ServiceBusClient
    {
        public CancelOutcomeServiceBusSender? CreatedSender { get; private set; }

        public override string FullyQualifiedNamespace => "fake.servicebus.windows.net";

        public override ServiceBusSender CreateSender(string queueOrTopicName)
            => CreatedSender = new CancelOutcomeServiceBusSender(queueOrTopicName);

        public override ServiceBusSender CreateSender(string queueOrTopicName, ServiceBusSenderOptions options)
            => CreateSender(queueOrTopicName);

        public override ValueTask DisposeAsync() => default;
    }

    /// <summary>
    /// A <see cref="ServiceBusSender"/> test double whose
    /// <see cref="CancelScheduledMessageAsync(long, CancellationToken)"/> throws a caller-supplied
    /// <see cref="ServiceBusException"/> instead of contacting a live namespace.
    /// </summary>
    private sealed class CancelOutcomeServiceBusSender(string entityPath) : ServiceBusSender
    {
        private bool _closed;

        public override string EntityPath { get; } = entityPath;

        public override bool IsClosed => _closed;

        public int CancelScheduledMessageCallCount { get; private set; }

        public ServiceBusException? CancelFailure { get; set; }

        public override Task<long> ScheduleMessageAsync(
            ServiceBusMessage message,
            DateTimeOffset scheduledEnqueueTime,
            CancellationToken cancellationToken = default)
            => Task.FromResult(1L);

        public override Task CancelScheduledMessageAsync(
            long sequenceNumber,
            CancellationToken cancellationToken = default)
        {
            CancelScheduledMessageCallCount++;

            if (CancelFailure is { } exception)
            {
                throw exception;
            }

            return Task.CompletedTask;
        }

        public override ValueTask DisposeAsync()
        {
            _closed = true;
            return default;
        }
    }
}
