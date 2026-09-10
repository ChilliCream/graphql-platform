using Azure.Messaging.ServiceBus;
using Mocha;
using Mocha.Middlewares;

namespace Mocha.Transport.AzureServiceBus.Tests;

public sealed class AzureServiceBusMessageEnvelopeParserTests
{
    [Fact]
    public void ParseAndCreate_Should_PreserveNativeProperties_When_MessageIsRedispatched()
    {
        var received = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"),
            messageId: "message-1",
            partitionKey: "session-1",
            sessionId: "session-1",
            replyToSessionId: "reply-session",
            to: "forward-target");

        var envelope = AzureServiceBusMessageEnvelopeParser.Instance.Parse(received);
        var redispatched = AzureServiceBusMessageFactory.Create(envelope, DateTimeOffset.UtcNow);

        Assert.Equal("session-1", GetHeader(envelope, AzureServiceBusMessageHeaders.SessionId));
        Assert.Equal("session-1", GetHeader(envelope, AzureServiceBusMessageHeaders.PartitionKey));
        Assert.Equal("reply-session", GetHeader(envelope, AzureServiceBusMessageHeaders.ReplyToSessionId));
        Assert.Equal("forward-target", GetHeader(envelope, AzureServiceBusMessageHeaders.To));
        Assert.Equal("session-1", redispatched.SessionId);
        Assert.Equal("session-1", redispatched.PartitionKey);
        Assert.Equal("reply-session", redispatched.ReplyToSessionId);
        Assert.Equal("forward-target", redispatched.To);
    }

    [Fact]
    public void Parse_Should_PreserveEveryEnclosedMessageType_When_MoreThanStackBufferCapacity()
    {
        var expected = Enumerable.Range(0, 40).Select(i => $"Type{i}").ToArray();
        var received = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"),
            messageId: "message-1",
            properties: new Dictionary<string, object>
            {
                [MessageHeaders.Transport.EnclosedMessageTypes.Key] = string.Join(';', expected)
            });

        var envelope = AzureServiceBusMessageEnvelopeParser.Instance.Parse(received);

        Assert.Equal(expected, envelope.EnclosedMessageTypes);
    }

    [Fact]
    public void ParseAndCreate_Should_PreserveUnknownXPrefixedApplicationProperty()
    {
        // arrange
        var received = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"),
            messageId: "message-1",
            properties: new Dictionary<string, object> { ["x-mocha-custom"] = "custom-value" });

        // act
        var envelope = AzureServiceBusMessageEnvelopeParser.Instance.Parse(received);
        var redispatched = AzureServiceBusMessageFactory.Create(envelope, DateTimeOffset.UtcNow);

        // assert
        Assert.Equal("custom-value", GetHeader(envelope, "x-mocha-custom"));
        Assert.Equal("custom-value", redispatched.ApplicationProperties["x-mocha-custom"]);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(0, 0)]
    public void Parse_Should_NormalizeDeliveryCountToZeroBased_When_MessageReceived(int sdkCount, int expectedCount)
    {
        // arrange
        var received = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"),
            messageId: "message-1",
            deliveryCount: sdkCount);

        // act
        var envelope = AzureServiceBusMessageEnvelopeParser.Instance.Parse(received);

        // assert
        Assert.Equal(expectedCount, envelope.DeliveryCount);
    }

    [Fact]
    public void Parse_Should_SurfaceDateTimeHeadersAsDateTimeOffset_When_PropertiesCarryDateValues()
    {
        // arrange
        var utcDateTime = new DateTime(2024, 3, 10, 8, 15, 30, DateTimeKind.Utc);
        var dateTimeOffset = new DateTimeOffset(2024, 3, 10, 8, 15, 30, TimeSpan.FromHours(2));
        var received = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"),
            messageId: "message-1",
            properties: new Dictionary<string, object>
            {
                ["x-created-at"] = utcDateTime,
                ["x-scheduled-at"] = dateTimeOffset
            });

        // act
        var envelope = AzureServiceBusMessageEnvelopeParser.Instance.Parse(received);
        envelope.Headers!.TryGetValue("x-created-at", out var createdAtValue);
        envelope.Headers.TryGetValue("x-scheduled-at", out var scheduledAtValue);

        // assert
        var createdAt = Assert.IsType<DateTimeOffset>(createdAtValue);
        var scheduledAt = Assert.IsType<DateTimeOffset>(scheduledAtValue);
        Assert.Equal(utcDateTime.Ticks, createdAt.UtcTicks);
        Assert.Equal(dateTimeOffset.UtcTicks, scheduledAt.UtcTicks);
    }

    [Fact]
    public void Create_Should_StoreDateTimeHeadersNatively_When_HeadersCarryDateValues()
    {
        // arrange
        var createdAt = new DateTimeOffset(2024, 3, 10, 8, 15, 30, TimeSpan.FromHours(2));
        var updatedAt = new DateTime(2024, 3, 10, 8, 15, 30, DateTimeKind.Utc);
        var headers = new Headers();
        headers.Set("x-created-at", createdAt);
        headers.Set("x-updated-at", updatedAt);
        var envelope = new MessageEnvelope
        {
            MessageId = "message-1",
            Body = BinaryData.FromString("{}").ToMemory(),
            Headers = headers
        };

        // act
        var message = AzureServiceBusMessageFactory.Create(envelope, DateTimeOffset.UtcNow);

        // assert
        var actualCreatedAt = Assert.IsType<DateTimeOffset>(message.ApplicationProperties["x-created-at"]);
        var actualUpdatedAt = Assert.IsType<DateTime>(message.ApplicationProperties["x-updated-at"]);
        Assert.Equal(createdAt, actualCreatedAt);
        Assert.Equal(updatedAt, actualUpdatedAt);
    }

    [Fact]
    public void Create_Should_LeaveMessageIdUnset_When_EnvelopeMessageIdIsNull()
    {
        // arrange
        var envelope = new MessageEnvelope { MessageId = null, Body = BinaryData.FromString("{}").ToMemory() };

        // act
        var message = AzureServiceBusMessageFactory.Create(envelope, DateTimeOffset.UtcNow);

        // assert
        Assert.Null(message.MessageId);
    }

    [Fact]
    public void Create_Should_SetMessageId_When_EnvelopeMessageIdIsProvided()
    {
        // arrange
        var envelope = new MessageEnvelope { MessageId = "message-1", Body = BinaryData.FromString("{}").ToMemory() };

        // act
        var message = AzureServiceBusMessageFactory.Create(envelope, DateTimeOffset.UtcNow);

        // assert
        Assert.Equal("message-1", message.MessageId);
    }

    [Fact]
    public void Parse_Should_FallBackToEnqueuedTime_When_SentAtHeaderIsMissing()
    {
        // arrange
        var enqueuedTime = new DateTimeOffset(2024, 5, 1, 12, 0, 0, TimeSpan.Zero);
        var received = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"),
            messageId: "message-1",
            enqueuedTime: enqueuedTime);

        // act
        var envelope = AzureServiceBusMessageEnvelopeParser.Instance.Parse(received);

        // assert
        Assert.Equal(enqueuedTime, envelope.SentAt);
    }

    [Fact]
    public void Parse_Should_FallBackToEnqueuedTime_When_SentAtHeaderIsNotALong()
    {
        // arrange
        var enqueuedTime = new DateTimeOffset(2024, 5, 1, 12, 0, 0, TimeSpan.Zero);
        var received = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"),
            messageId: "message-1",
            properties: new Dictionary<string, object> { [AzureServiceBusMessageHeaders.SentAt] = "not-a-long" },
            enqueuedTime: enqueuedTime);

        // act
        var envelope = AzureServiceBusMessageEnvelopeParser.Instance.Parse(received);

        // assert
        Assert.Equal(enqueuedTime, envelope.SentAt);
    }

    [Fact]
    public void Parse_Should_PreferSubject_When_SubjectAndMessageTypePropertyBothSet()
    {
        // arrange
        var received = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"),
            messageId: "message-1",
            subject: "SubjectType",
            properties: new Dictionary<string, object> { [MessageHeaders.Transport.MessageType.Key] = "PropertyType" });

        // act
        var envelope = AzureServiceBusMessageEnvelopeParser.Instance.Parse(received);

        // assert
        Assert.Equal("SubjectType", envelope.MessageType);
    }

    [Fact]
    public void Parse_Should_FallBackToMessageTypeProperty_When_SubjectIsNull()
    {
        // arrange
        var received = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"),
            messageId: "message-1",
            properties: new Dictionary<string, object> { [MessageHeaders.Transport.MessageType.Key] = "PropertyType" });

        // act
        var envelope = AzureServiceBusMessageEnvelopeParser.Instance.Parse(received);

        // assert
        Assert.Equal("PropertyType", envelope.MessageType);
    }

    [Fact]
    public void Parse_Should_SetDeliverBy_When_ExpiresAtIsFinite()
    {
        // arrange
        var enqueuedTime = new DateTimeOffset(2024, 5, 1, 12, 0, 0, TimeSpan.Zero);
        var timeToLive = TimeSpan.FromMinutes(5);
        var received = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"),
            messageId: "message-1",
            timeToLive: timeToLive,
            enqueuedTime: enqueuedTime);

        // act
        var envelope = AzureServiceBusMessageEnvelopeParser.Instance.Parse(received);

        // assert
        Assert.Equal(enqueuedTime.Add(timeToLive), envelope.DeliverBy);
    }

    [Fact]
    public void Parse_Should_ReturnNullDeliverBy_When_ExpiresAtIsMaxValue()
    {
        // arrange
        var received = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"),
            messageId: "message-1");

        // act
        var envelope = AzureServiceBusMessageEnvelopeParser.Instance.Parse(received);

        // assert
        Assert.Null(envelope.DeliverBy);
    }

    [Fact]
    public void ParseAndCreate_Should_RoundTripAllCommonAndNativeProperties_When_EnvelopeFieldsAreFullyPopulated()
    {
        // arrange
        var enqueuedTime = new DateTimeOffset(2024, 5, 1, 12, 0, 0, TimeSpan.Zero);
        var sentAt = enqueuedTime.AddMinutes(-1);
        var timeToLive = TimeSpan.FromMinutes(5);
        var received = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{\"orderId\":\"ORD-1\"}"),
            messageId: "message-1",
            partitionKey: "session-1",
            sessionId: "session-1",
            replyToSessionId: "reply-session",
            timeToLive: timeToLive,
            correlationId: "correlation-1",
            subject: "OrderCreated",
            to: "forward-target",
            contentType: "application/json",
            replyTo: "reply-queue",
            properties: new Dictionary<string, object>
            {
                [MessageHeaders.Transport.ConversationId.Key] = "conversation-1",
                [MessageHeaders.Transport.CausationId.Key] = "causation-1",
                [MessageHeaders.Transport.SourceAddress.Key] = "source-address",
                [MessageHeaders.Transport.DestinationAddress.Key] = "destination-address",
                [MessageHeaders.Transport.FaultAddress.Key] = "fault-address",
                [MessageHeaders.Transport.EnclosedMessageTypes.Key] = "OrderCreated;IEvent",
                [AzureServiceBusMessageHeaders.SentAt] = sentAt.ToUnixTimeMilliseconds()
            },
            enqueuedTime: enqueuedTime);

        // act
        var envelope = AzureServiceBusMessageEnvelopeParser.Instance.Parse(received);
        var redispatched = AzureServiceBusMessageFactory.Create(envelope, DateTimeOffset.UtcNow);

        // assert
        Assert.Equal(
            (
                "message-1",
                "correlation-1",
                "conversation-1",
                "causation-1",
                "source-address",
                "destination-address",
                "reply-queue",
                "fault-address",
                "application/json",
                "OrderCreated",
                sentAt,
                enqueuedTime.Add(timeToLive)
            ),
            (
                envelope.MessageId,
                envelope.CorrelationId,
                envelope.ConversationId,
                envelope.CausationId,
                envelope.SourceAddress,
                envelope.DestinationAddress,
                envelope.ResponseAddress,
                envelope.FaultAddress,
                envelope.ContentType,
                envelope.MessageType,
                envelope.SentAt,
                envelope.DeliverBy
            ));
        Assert.Equal(["OrderCreated", "IEvent"], envelope.EnclosedMessageTypes);

        Assert.Equal(
            ("session-1", "session-1", "reply-session", "forward-target"),
            (
                GetHeader(envelope, AzureServiceBusMessageHeaders.SessionId),
                GetHeader(envelope, AzureServiceBusMessageHeaders.PartitionKey),
                GetHeader(envelope, AzureServiceBusMessageHeaders.ReplyToSessionId),
                GetHeader(envelope, AzureServiceBusMessageHeaders.To)));

        Assert.Equal(
            (
                "correlation-1",
                "OrderCreated",
                "reply-queue",
                "application/json",
                "session-1",
                "session-1",
                "reply-session",
                "forward-target"
            ),
            (
                redispatched.CorrelationId,
                redispatched.Subject,
                redispatched.ReplyTo,
                redispatched.ContentType,
                redispatched.SessionId,
                redispatched.PartitionKey,
                redispatched.ReplyToSessionId,
                redispatched.To
            ));

        Assert.Equal(
            (
                "conversation-1",
                "causation-1",
                "source-address",
                "destination-address",
                "fault-address",
                "OrderCreated",
                "OrderCreated;IEvent",
                sentAt.ToUnixTimeMilliseconds()
            ),
            (
                (string)redispatched.ApplicationProperties[MessageHeaders.Transport.ConversationId.Key],
                (string)redispatched.ApplicationProperties[MessageHeaders.Transport.CausationId.Key],
                (string)redispatched.ApplicationProperties[MessageHeaders.Transport.SourceAddress.Key],
                (string)redispatched.ApplicationProperties[MessageHeaders.Transport.DestinationAddress.Key],
                (string)redispatched.ApplicationProperties[MessageHeaders.Transport.FaultAddress.Key],
                (string)redispatched.ApplicationProperties[MessageHeaders.Transport.MessageType.Key],
                (string)redispatched.ApplicationProperties[MessageHeaders.Transport.EnclosedMessageTypes.Key],
                (long)redispatched.ApplicationProperties[AzureServiceBusMessageHeaders.SentAt]
            ));
    }

    [Fact]
    public void ParseAndCreate_Should_PreserveByteArrayApplicationProperty()
    {
        // arrange
        var bytes = new byte[] { 1, 2, 3, 4 };
        var received = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"),
            messageId: "message-1",
            properties: new Dictionary<string, object> { ["x-signature"] = bytes });

        // act
        var envelope = AzureServiceBusMessageEnvelopeParser.Instance.Parse(received);
        var redispatched = AzureServiceBusMessageFactory.Create(envelope, DateTimeOffset.UtcNow);

        // assert
        Assert.Equal(bytes, GetHeader(envelope, "x-signature"));
        Assert.Equal(bytes, redispatched.ApplicationProperties["x-signature"]);
    }

    [Fact]
    public void Create_Should_OmitApplicationProperty_When_HeaderValueIsNull()
    {
        // arrange
        var headers = new Headers();
        headers.Set<object?>("x-optional", null);
        var envelope = new MessageEnvelope
        {
            MessageId = "message-1",
            Body = BinaryData.FromString("{}").ToMemory(),
            Headers = headers
        };

        // act
        var message = AzureServiceBusMessageFactory.Create(envelope, DateTimeOffset.UtcNow);

        // assert
        Assert.False(message.ApplicationProperties.ContainsKey("x-optional"));
    }

    [Fact]
    public void Create_Should_IgnoreCustomHeader_When_KeyCollidesWithFrameworkMessageTypeHeader()
    {
        // arrange
        var headers = new Headers();
        headers.Set(MessageHeaders.Transport.MessageType.Key, "spoofed-type");
        var envelope = new MessageEnvelope
        {
            MessageId = "message-1",
            MessageType = "real-type",
            Body = BinaryData.FromString("{}").ToMemory(),
            Headers = headers
        };

        // act
        var message = AzureServiceBusMessageFactory.Create(envelope, DateTimeOffset.UtcNow);

        // assert
        Assert.Equal("real-type", message.Subject);
        Assert.Equal("real-type", message.ApplicationProperties[MessageHeaders.Transport.MessageType.Key]);
    }

    [Fact]
    public void Create_Should_IgnoreCustomHeader_When_KeyCollidesWithFrameworkSentAtHeader()
    {
        // arrange
        var sentAt = new DateTimeOffset(2024, 5, 1, 12, 0, 0, TimeSpan.Zero);
        var headers = new Headers();
        headers.Set(AzureServiceBusMessageHeaders.SentAt, 1L);
        var envelope = new MessageEnvelope
        {
            MessageId = "message-1",
            SentAt = sentAt,
            Body = BinaryData.FromString("{}").ToMemory(),
            Headers = headers
        };

        // act
        var message = AzureServiceBusMessageFactory.Create(envelope, DateTimeOffset.UtcNow);

        // assert
        Assert.Equal(sentAt.ToUnixTimeMilliseconds(), message.ApplicationProperties[AzureServiceBusMessageHeaders.SentAt]);
    }

    [Fact]
    public void ParseAndCreate_Should_PreserveSimilarlyPrefixedCustomHeaders_When_KeysAreCloseToFrameworkNames()
    {
        // arrange
        var received = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"),
            messageId: "message-1",
            properties: new Dictionary<string, object>
            {
                ["x-sent-at-custom"] = "custom-sent-at",
                ["x-to-2"] = "custom-to",
                ["x-session-id-suffix"] = "custom-session"
            });

        // act
        var envelope = AzureServiceBusMessageEnvelopeParser.Instance.Parse(received);
        var redispatched = AzureServiceBusMessageFactory.Create(envelope, DateTimeOffset.UtcNow);

        // assert
        Assert.Equal("custom-sent-at", GetHeader(envelope, "x-sent-at-custom"));
        Assert.Equal("custom-to", GetHeader(envelope, "x-to-2"));
        Assert.Equal("custom-session", GetHeader(envelope, "x-session-id-suffix"));
        Assert.Equal("custom-sent-at", redispatched.ApplicationProperties["x-sent-at-custom"]);
    }

    [Fact]
    public void Parse_Should_ReturnNullConversationId_When_PropertyIsNotAString()
    {
        // arrange
        var received = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"),
            messageId: "message-1",
            properties: new Dictionary<string, object> { [MessageHeaders.Transport.ConversationId.Key] = 123 });

        // act
        var envelope = AzureServiceBusMessageEnvelopeParser.Instance.Parse(received);

        // assert
        Assert.Null(envelope.ConversationId);
    }

    [Fact]
    public void Create_Should_IgnoreSessionIdHeader_When_ValueIsNotAString()
    {
        // arrange
        var headers = new Headers();
        headers.Set(AzureServiceBusMessageHeaders.SessionId, 42);
        var envelope = new MessageEnvelope
        {
            MessageId = "message-1",
            Body = BinaryData.FromString("{}").ToMemory(),
            Headers = headers
        };

        // act
        var message = AzureServiceBusMessageFactory.Create(envelope, DateTimeOffset.UtcNow);

        // assert
        Assert.Null(message.SessionId);
    }

    [Fact]
    public void Create_Should_Throw_When_PartitionKeyDoesNotEqualSessionId()
    {
        // arrange
        var headers = new Headers();
        headers.Set(AzureServiceBusMessageHeaders.SessionId, "session-1");
        headers.Set(AzureServiceBusMessageHeaders.PartitionKey, "different-partition");
        var envelope = new MessageEnvelope
        {
            MessageId = "message-1",
            Body = BinaryData.FromString("{}").ToMemory(),
            Headers = headers
        };

        // act
        var exception = Record.Exception(() => AzureServiceBusMessageFactory.Create(envelope, DateTimeOffset.UtcNow));

        // assert
        Assert.Equal(
            "PartitionKey must equal SessionId when both are set on an Azure Service Bus message.",
            Assert.IsType<InvalidOperationException>(exception).Message);
    }

    private static object? GetHeader(MessageEnvelope envelope, string key)
    {
        Assert.NotNull(envelope.Headers);
        Assert.True(envelope.Headers.TryGetValue(key, out var value));
        return value;
    }
}
