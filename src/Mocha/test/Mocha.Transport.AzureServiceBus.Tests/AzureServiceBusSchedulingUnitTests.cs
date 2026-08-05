using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;
using Mocha.Scheduling;
using Mocha.Transport.AzureServiceBus.Scheduling;

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
    public void CreateMessage_Should_MapEnvelopeAndNativeProperties_When_MessageIsScheduled()
    {
        var headers = new Headers();
        headers.Set(AzureServiceBusMessageHeaders.SessionId, "session-1");
        headers.Set(AzureServiceBusMessageHeaders.PartitionKey, "session-1");
        headers.Set(AzureServiceBusMessageHeaders.ReplyToSessionId, "reply-session");
        headers.Set(AzureServiceBusMessageHeaders.To, "forward-target");
        headers.Set("custom", "value");
        headers.Set("custom-date", new DateTimeOffset(2026, 7, 13, 10, 0, 0, TimeSpan.Zero));

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
        Assert.Equal("conversation-1", properties[AzureServiceBusMessageHeaders.ConversationId]);
        Assert.Equal("causation-1", properties[AzureServiceBusMessageHeaders.CausationId]);
        Assert.Equal("azuresb://example/q/source", properties[AzureServiceBusMessageHeaders.SourceAddress]);
        Assert.Equal("azuresb://example/q/orders", properties[AzureServiceBusMessageHeaders.DestinationAddress]);
        Assert.Equal("azuresb://example/q/orders_error", properties[AzureServiceBusMessageHeaders.FaultAddress]);
        Assert.Equal("OrderCreated", properties[AzureServiceBusMessageHeaders.MessageType]);
        Assert.Equal("OrderCreated;IEvent", properties[AzureServiceBusMessageHeaders.EnclosedMessageTypes]);
        Assert.Equal(
            enqueueTime.AddMinutes(-1).ToUnixTimeMilliseconds(),
            properties[AzureServiceBusMessageHeaders.SentAt]);
        Assert.Equal("value", properties["custom"]);
        Assert.Equal(1783936800000, properties["custom-date"]);
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
}
