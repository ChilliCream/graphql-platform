using Azure.Messaging.ServiceBus;

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

    private static object? GetHeader(Mocha.Middlewares.MessageEnvelope envelope, string key)
    {
        Assert.NotNull(envelope.Headers);
        Assert.True(envelope.Headers.TryGetValue(key, out var value));
        return value;
    }
}
