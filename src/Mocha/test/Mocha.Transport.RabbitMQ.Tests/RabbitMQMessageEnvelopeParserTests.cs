using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mocha.Transport.RabbitMQ.Tests;

public class RabbitMQMessageEnvelopeParserTests
{
    private readonly RabbitMQMessageEnvelopeParser _parser = RabbitMQMessageEnvelopeParser.Instance;

    [Fact]
    public void Parse_Should_ExtractMessageId_When_PropertySet()
    {
        // arrange
        var args = CreateDeliverEventArgs(props => props.MessageId = "msg-123");

        // act
        var envelope = _parser.Parse(args);

        // assert
        Assert.Equal("msg-123", envelope.MessageId);
    }

    [Fact]
    public void Parse_Should_ExtractCorrelationId_When_PropertySet()
    {
        // arrange
        var args = CreateDeliverEventArgs(props => props.CorrelationId = "corr-456");

        // act
        var envelope = _parser.Parse(args);

        // assert
        Assert.Equal("corr-456", envelope.CorrelationId);
    }

    [Fact]
    public void Parse_Should_ExtractResponseAddress_When_ReplyToSet()
    {
        // arrange
        var args = CreateDeliverEventArgs(props => props.ReplyTo = "reply-queue");

        // act
        var envelope = _parser.Parse(args);

        // assert
        Assert.Equal("reply-queue", envelope.ResponseAddress);
    }

    [Fact]
    public void Parse_Should_ExtractContentType_When_PropertySet()
    {
        // arrange
        var args = CreateDeliverEventArgs(props => props.ContentType = "application/json");

        // act
        var envelope = _parser.Parse(args);

        // assert
        Assert.Equal("application/json", envelope.ContentType);
    }

    [Fact]
    public void Parse_Should_ExtractMessageType_When_TypePropertySet()
    {
        // arrange
        var args = CreateDeliverEventArgs(props => props.Type = "OrderCreated");

        // act
        var envelope = _parser.Parse(args);

        // assert
        Assert.Equal("OrderCreated", envelope.MessageType);
    }

    [Fact]
    public void Parse_Should_FallbackToHeader_When_TypePropertyNull()
    {
        // arrange
        var args = CreateDeliverEventArgs(props => props.Headers = new Dictionary<string, object?> { ["x-message-type"] = "FallbackType"u8.ToArray() });

        // act
        var envelope = _parser.Parse(args);

        // assert
        Assert.Equal("FallbackType", envelope.MessageType);
    }

    [Fact]
    public void Parse_Should_ExtractTimestamp_When_UnixTimePositive()
    {
        // arrange
        var args = CreateDeliverEventArgs(props => props.Timestamp = new AmqpTimestamp(1700000000));

        // act
        var envelope = _parser.Parse(args);

        // assert
        Assert.NotNull(envelope.SentAt);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1700000000), envelope.SentAt.Value);
    }

    [Fact]
    public void Parse_Should_ReturnNullSentAt_When_TimestampZero()
    {
        // arrange
        var args = CreateDeliverEventArgs(props => props.Timestamp = new AmqpTimestamp(0));

        // act
        var envelope = _parser.Parse(args);

        // assert
        Assert.Null(envelope.SentAt);
    }

    [Fact]
    public void Parse_Should_ExtractBody_When_BodyPresent()
    {
        // arrange
        var body = "{\"orderId\":\"ORD-1\"}"u8.ToArray();
        var args = CreateDeliverEventArgs(body: body);

        // act
        var envelope = _parser.Parse(args);

        // assert
        Assert.Equal(body, envelope.Body.ToArray());
    }

    [Fact]
    public void Parse_Should_ExtractCustomHeaders_When_HeadersPresent()
    {
        // arrange
        var args = CreateDeliverEventArgs(props => props.Headers = new Dictionary<string, object?> { ["x-custom"] = "custom-value"u8.ToArray() });

        // act
        var envelope = _parser.Parse(args);

        // assert
        Assert.True(envelope.Headers!.TryGetValue("x-custom", out var value));
        Assert.Equal("custom-value", value);
    }

    [Fact]
    public void Parse_Should_DecodeByteArrayHeaders_When_Utf8Bytes()
    {
        // arrange
        var args = CreateDeliverEventArgs(props =>
        {
            props.Headers = new Dictionary<string, object?>
            {
                ["x-source-address"] = "rabbitmq:///q/source"u8.ToArray()
            };
        });

        // act
        var envelope = _parser.Parse(args);

        // assert
        Assert.Equal("rabbitmq:///q/source", envelope.SourceAddress);
    }

    [Fact]
    public void Parse_Should_ConvertAmqpTimestamp_When_HeaderIsTimestamp()
    {
        // arrange
        var args = CreateDeliverEventArgs(props => props.Headers = new Dictionary<string, object?> { ["x-timestamp"] = new AmqpTimestamp(1700000000) });

        // act
        var envelope = _parser.Parse(args);

        // assert
        Assert.True(envelope.Headers!.TryGetValue("x-timestamp", out var ts));
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1700000000), ts);
    }

    [Fact]
    public void Parse_Should_SetDeliveryCountOne_When_Redelivered()
    {
        // arrange
        var args = CreateDeliverEventArgs(redelivered: true);

        // act
        var envelope = _parser.Parse(args);

        // assert
        Assert.Equal(1, envelope.DeliveryCount);
    }

    [Fact]
    public void Parse_Should_SetDeliveryCountZero_When_NotRedelivered()
    {
        // arrange
        var args = CreateDeliverEventArgs(redelivered: false);

        // act
        var envelope = _parser.Parse(args);

        // assert
        Assert.Equal(0, envelope.DeliveryCount);
    }

    [Fact]
    public void Parse_Should_UseXDeliveryCount_When_QuorumQueueHeaderPresent()
    {
        // arrange - quorum queues set x-delivery-count as a long
        var args = CreateDeliverEventArgs(
            props => props.Headers = new Dictionary<string, object?>
            {
                ["x-delivery-count"] = 3L
            },
            redelivered: true);

        // act
        var envelope = _parser.Parse(args);

        // assert - exact count from header, not the boolean fallback
        Assert.Equal(3, envelope.DeliveryCount);
    }

    [Fact]
    public void Parse_Should_UseXDeliveryCount_When_ValueIsZero()
    {
        // arrange - first delivery on a quorum queue
        var args = CreateDeliverEventArgs(
            props => props.Headers = new Dictionary<string, object?>
            {
                ["x-delivery-count"] = 0L
            },
            redelivered: false);

        // act
        var envelope = _parser.Parse(args);

        // assert
        Assert.Equal(0, envelope.DeliveryCount);
    }

    [Fact]
    public void Parse_Should_FallbackToRedelivered_When_XDeliveryCountAbsent()
    {
        // arrange - classic queue, no x-delivery-count header
        var args = CreateDeliverEventArgs(
            props => props.Headers = new Dictionary<string, object?>
            {
                ["x-some-other-header"] = "value"u8.ToArray()
            },
            redelivered: true);

        // act
        var envelope = _parser.Parse(args);

        // assert - falls back to redelivered flag
        Assert.Equal(1, envelope.DeliveryCount);
    }

    [Fact]
    public void Parse_Should_ExtractEnclosedMessageTypes_When_HeaderPresent()
    {
        // arrange
        var args = CreateDeliverEventArgs(props =>
        {
            props.Headers = new Dictionary<string, object?>
            {
                ["x-enclosed-message-types"] = new List<object> { "OrderCreated"u8.ToArray(), "IEvent"u8.ToArray() }
            };
        });

        // act
        var envelope = _parser.Parse(args);

        // assert
        Assert.NotNull(envelope.EnclosedMessageTypes);
        Assert.Equal(2, envelope.EnclosedMessageTypes!.Value.Length);
        Assert.Equal("OrderCreated", envelope.EnclosedMessageTypes.Value[0]);
        Assert.Equal("IEvent", envelope.EnclosedMessageTypes.Value[1]);
    }

    [Fact]
    public void Parse_Should_ComputeDeliverByFromSentAt_When_TimestampAndExpirationPresent()
    {
        // arrange
        const long sentAtUnix = 1700000000L;
        var sentAt = DateTimeOffset.FromUnixTimeSeconds(sentAtUnix);
        var args = CreateDeliverEventArgs(props =>
        {
            props.Timestamp = new AmqpTimestamp(sentAtUnix);
            props.Expiration = "60000"; // 60 seconds TTL
        });

        // act
        var envelope = _parser.Parse(args);

        // assert
        Assert.NotNull(envelope.DeliverBy);
        Assert.Equal(sentAt.AddMilliseconds(60000), envelope.DeliverBy.Value);
    }

    [Fact]
    public void Parse_Should_FallbackToUtcNow_When_ExpirationPresentButNoTimestamp()
    {
        // arrange
        var args = CreateDeliverEventArgs(props =>
        {
            props.Expiration = "60000"; // 60 seconds
        });

        // act
        var beforeParse = DateTimeOffset.UtcNow;
        var envelope = _parser.Parse(args);
        var afterParse = DateTimeOffset.UtcNow;

        // assert
        Assert.NotNull(envelope.DeliverBy);
        var expectedMin = beforeParse.AddMilliseconds(60000);
        var expectedMax = afterParse.AddMilliseconds(60000);
        Assert.InRange(envelope.DeliverBy.Value, expectedMin, expectedMax);
    }

    [Fact]
    public void Parse_Should_ReturnNullDeliverBy_When_ExpirationEmpty()
    {
        // arrange
        var args = CreateDeliverEventArgs(props => props.Expiration = null);

        // act
        var envelope = _parser.Parse(args);

        // assert
        Assert.Null(envelope.DeliverBy);
    }

    [Fact]
    public void Parse_Should_ExtractConversationId_When_HeaderPresent()
    {
        // arrange
        var args = CreateDeliverEventArgs(props => props.Headers = new Dictionary<string, object?> { ["x-conversation-id"] = "conv-789"u8.ToArray() });

        // act
        var envelope = _parser.Parse(args);

        // assert
        Assert.Equal("conv-789", envelope.ConversationId);
    }

    [Fact]
    public void Parse_Should_ExtractCausationId_When_HeaderPresent()
    {
        // arrange
        var args = CreateDeliverEventArgs(props => props.Headers = new Dictionary<string, object?> { ["x-causation-id"] = "cause-101"u8.ToArray() });

        // act
        var envelope = _parser.Parse(args);

        // assert
        Assert.Equal("cause-101", envelope.CausationId);
    }

    [Fact]
    public void Parse_Should_ReturnEmptyHeaders_When_NoHeaders()
    {
        // arrange
        var args = CreateDeliverEventArgs();

        // act
        var envelope = _parser.Parse(args);

        // assert
        Assert.NotNull(envelope.Headers);
        Assert.Equal(0, envelope.Headers.Count);
    }

    private static BasicDeliverEventArgs CreateDeliverEventArgs(
        Action<BasicProperties>? configureProps = null,
        byte[]? body = null,
        bool redelivered = false)
    {
        var props = new BasicProperties();
        configureProps?.Invoke(props);

        return new BasicDeliverEventArgs(
            consumerTag: "test-consumer",
            deliveryTag: 1,
            redelivered: redelivered,
            exchange: "test-exchange",
            routingKey: "test-key",
            properties: props,
            body: body ?? ReadOnlyMemory<byte>.Empty);
    }
}
