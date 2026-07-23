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
                [AzureServiceBusMessageHeaders.EnclosedMessageTypes] = string.Join(';', expected)
            });

        var envelope = AzureServiceBusMessageEnvelopeParser.Instance.Parse(received);

        Assert.Equal(expected, envelope.EnclosedMessageTypes);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(0, 0)]
    public void Parse_Should_NormalizeDeliveryCountToZeroBased_When_MessageReceived(
        int sdkCount,
        int expectedCount)
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
        var dateTimeOffset = new DateTimeOffset(
            2024,
            3,
            10,
            8,
            15,
            30,
            TimeSpan.FromHours(2));
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
        var createdAt = new DateTimeOffset(
            2024,
            3,
            10,
            8,
            15,
            30,
            TimeSpan.FromHours(2));
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
        var actualCreatedAt = Assert.IsType<DateTimeOffset>(
            message.ApplicationProperties["x-created-at"]);
        var actualUpdatedAt = Assert.IsType<DateTime>(
            message.ApplicationProperties["x-updated-at"]);
        Assert.Equal(createdAt, actualCreatedAt);
        Assert.Equal(updatedAt, actualUpdatedAt);
    }

    [Fact]
    public void Create_Should_LeaveMessageIdUnset_When_EnvelopeMessageIdIsNull()
    {
        // arrange
        var envelope = new MessageEnvelope
        {
            MessageId = null,
            Body = BinaryData.FromString("{}").ToMemory()
        };

        // act
        var message = AzureServiceBusMessageFactory.Create(envelope, DateTimeOffset.UtcNow);

        // assert
        Assert.Null(message.MessageId);
    }

    [Fact]
    public void Create_Should_SetMessageId_When_EnvelopeMessageIdIsProvided()
    {
        // arrange
        var envelope = new MessageEnvelope
        {
            MessageId = "message-1",
            Body = BinaryData.FromString("{}").ToMemory()
        };

        // act
        var message = AzureServiceBusMessageFactory.Create(envelope, DateTimeOffset.UtcNow);

        // assert
        Assert.Equal("message-1", message.MessageId);
    }

    private static object? GetHeader(Mocha.Middlewares.MessageEnvelope envelope, string key)
    {
        Assert.NotNull(envelope.Headers);
        Assert.True(envelope.Headers.TryGetValue(key, out var value));
        return value;
    }
}
