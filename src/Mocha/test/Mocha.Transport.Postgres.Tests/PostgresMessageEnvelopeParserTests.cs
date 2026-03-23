using System.Text;
using System.Text.Json;

namespace Mocha.Transport.Postgres.Tests;

public class PostgresMessageEnvelopeParserTests
{
    private readonly PostgresMessageEnvelopeParser _parser = PostgresMessageEnvelopeParser.Instance;

    [Fact]
    public void Parse_Should_ExtractMessageId_When_HeaderPresent()
    {
        // arrange
        var item = CreateMessageItem(headers: new { messageId = "msg-123" });

        // act
        var envelope = _parser.Parse(item);

        // assert
        Assert.Equal("msg-123", envelope.MessageId);
    }

    [Fact]
    public void Parse_Should_ExtractCorrelationId_When_HeaderPresent()
    {
        // arrange
        var item = CreateMessageItem(headers: new { correlationId = "corr-456" });

        // act
        var envelope = _parser.Parse(item);

        // assert
        Assert.Equal("corr-456", envelope.CorrelationId);
    }

    [Fact]
    public void Parse_Should_ExtractConversationId_When_HeaderPresent()
    {
        // arrange
        var item = CreateMessageItem(headers: new { conversationId = "conv-789" });

        // act
        var envelope = _parser.Parse(item);

        // assert
        Assert.Equal("conv-789", envelope.ConversationId);
    }

    [Fact]
    public void Parse_Should_ExtractCausationId_When_HeaderPresent()
    {
        // arrange
        var item = CreateMessageItem(headers: new { causationId = "cause-101" });

        // act
        var envelope = _parser.Parse(item);

        // assert
        Assert.Equal("cause-101", envelope.CausationId);
    }

    [Fact]
    public void Parse_Should_ExtractSourceAddress_When_HeaderPresent()
    {
        // arrange
        var item = CreateMessageItem(headers: new { sourceAddress = "postgres:///q/source" });

        // act
        var envelope = _parser.Parse(item);

        // assert
        Assert.Equal("postgres:///q/source", envelope.SourceAddress);
    }

    [Fact]
    public void Parse_Should_ExtractDestinationAddress_When_HeaderPresent()
    {
        // arrange
        var item = CreateMessageItem(headers: new { destinationAddress = "postgres:///q/destination" });

        // act
        var envelope = _parser.Parse(item);

        // assert
        Assert.Equal("postgres:///q/destination", envelope.DestinationAddress);
    }

    [Fact]
    public void Parse_Should_ExtractResponseAddress_When_HeaderPresent()
    {
        // arrange
        var item = CreateMessageItem(headers: new { responseAddress = "postgres:///q/reply" });

        // act
        var envelope = _parser.Parse(item);

        // assert
        Assert.Equal("postgres:///q/reply", envelope.ResponseAddress);
    }

    [Fact]
    public void Parse_Should_ExtractFaultAddress_When_HeaderPresent()
    {
        // arrange
        var item = CreateMessageItem(headers: new { faultAddress = "postgres:///q/fault" });

        // act
        var envelope = _parser.Parse(item);

        // assert
        Assert.Equal("postgres:///q/fault", envelope.FaultAddress);
    }

    [Fact]
    public void Parse_Should_ExtractContentType_When_HeaderPresent()
    {
        // arrange
        var item = CreateMessageItem(headers: new { contentType = "application/json" });

        // act
        var envelope = _parser.Parse(item);

        // assert
        Assert.Equal("application/json", envelope.ContentType);
    }

    [Fact]
    public void Parse_Should_ExtractMessageType_When_HeaderPresent()
    {
        // arrange
        var item = CreateMessageItem(headers: new { messageType = "OrderCreated" });

        // act
        var envelope = _parser.Parse(item);

        // assert
        Assert.Equal("OrderCreated", envelope.MessageType);
    }

    [Fact]
    public void Parse_Should_ExtractBody_When_BodyPresent()
    {
        // arrange
        var body = "{\"orderId\":\"ORD-1\"}"u8.ToArray();
        var item = CreateMessageItem(body: body);

        // act
        var envelope = _parser.Parse(item);

        // assert
        Assert.Equal(body, envelope.Body.ToArray());
    }

    [Fact]
    public void Parse_Should_ExtractSentAt_When_SentTimePresent()
    {
        // arrange
        var sentTime = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var item = CreateMessageItem(sentTime: sentTime);

        // act
        var envelope = _parser.Parse(item);

        // assert
        Assert.NotNull(envelope.SentAt);
        Assert.Equal(new DateTimeOffset(sentTime, TimeSpan.Zero), envelope.SentAt.Value);
    }

    [Fact]
    public void Parse_Should_ExtractDeliveryCount_When_Present()
    {
        // arrange
        var item = CreateMessageItem(deliveryCount: 3);

        // act
        var envelope = _parser.Parse(item);

        // assert
        Assert.Equal(3, envelope.DeliveryCount);
    }

    [Fact]
    public void Parse_Should_ReturnEmptyHeaders_When_NoHeaders()
    {
        // arrange
        var item = CreateMessageItem(headers: null);

        // act
        var envelope = _parser.Parse(item);

        // assert
        Assert.NotNull(envelope.Headers);
        Assert.Equal(0, envelope.Headers.Count);
    }

    [Fact]
    public void Parse_Should_ReturnEmptyHeaders_When_EmptyJson()
    {
        // arrange
        var item = new PostgresMessageItem(
            TransportMessageId: Guid.NewGuid(),
            Body: ReadOnlyMemory<byte>.Empty,
            Headers: ReadOnlyMemory<byte>.Empty,
            QueueId: 1,
            SentTime: DateTime.UtcNow,
            DeliveryCount: 0,
            MaxDeliveryCount: 10,
            ErrorReason: null);

        // act
        var envelope = _parser.Parse(item);

        // assert
        Assert.NotNull(envelope.Headers);
        Assert.Equal(0, envelope.Headers.Count);
    }

    [Fact]
    public void Parse_Should_ExtractEnclosedMessageTypes_When_HeaderPresent()
    {
        // arrange
        var item = CreateMessageItem(headers: new
        {
            enclosedMessageTypes = new[] { "OrderCreated", "IEvent" }
        });

        // act
        var envelope = _parser.Parse(item);

        // assert
        Assert.NotNull(envelope.EnclosedMessageTypes);
        Assert.Equal(2, envelope.EnclosedMessageTypes!.Value.Length);
        Assert.Equal("OrderCreated", envelope.EnclosedMessageTypes.Value[0]);
        Assert.Equal("IEvent", envelope.EnclosedMessageTypes.Value[1]);
    }

    [Fact]
    public void Parse_Should_ExtractCustomHeaders_When_Present()
    {
        // arrange
        var item = CreateMessageItem(headers: new
        {
            messageId = "msg-1",
            customHeader = "custom-value"
        });

        // act
        var envelope = _parser.Parse(item);

        // assert
        Assert.True(envelope.Headers!.TryGetValue("customHeader", out var value));
        Assert.Equal("custom-value", value);
    }

    [Fact]
    public void Parse_Should_HandleBooleanHeaders_When_Present()
    {
        // arrange
        var item = CreateMessageItem(headers: new { isRetry = true });

        // act
        var envelope = _parser.Parse(item);

        // assert
        Assert.True(envelope.Headers!.TryGetValue("isRetry", out var value));
        Assert.Equal(true, value);
    }

    [Fact]
    public void Parse_Should_HandleNumericHeaders_When_Present()
    {
        // arrange
        var item = CreateMessageItem(headers: new { retryCount = 5 });

        // act
        var envelope = _parser.Parse(item);

        // assert
        Assert.True(envelope.Headers!.TryGetValue("retryCount", out var value));
        Assert.Equal(5.0, value);
    }

    [Fact]
    public void Parse_Should_Throw_When_HeadersMalformed()
    {
        // arrange
        var item = new PostgresMessageItem(
            TransportMessageId: Guid.NewGuid(),
            Body: ReadOnlyMemory<byte>.Empty,
            Headers: "not-valid-json{"u8.ToArray(),
            QueueId: 1,
            SentTime: DateTime.UtcNow,
            DeliveryCount: 0,
            MaxDeliveryCount: 10,
            ErrorReason: null);

        // act & assert
        Assert.Throws<InvalidOperationException>(() => _parser.Parse(item));
    }

    [Fact]
    public void Parse_Should_ExtractAllStandardHeaders_When_AllPresent()
    {
        // arrange
        var item = CreateMessageItem(headers: new
        {
            messageId = "msg-all",
            correlationId = "corr-all",
            conversationId = "conv-all",
            causationId = "cause-all",
            sourceAddress = "postgres:///q/src",
            destinationAddress = "postgres:///q/dst",
            responseAddress = "postgres:///q/reply",
            faultAddress = "postgres:///q/fault",
            contentType = "application/json",
            messageType = "TestMessage"
        });

        // act
        var envelope = _parser.Parse(item);

        // assert
        Assert.Equal("msg-all", envelope.MessageId);
        Assert.Equal("corr-all", envelope.CorrelationId);
        Assert.Equal("conv-all", envelope.ConversationId);
        Assert.Equal("cause-all", envelope.CausationId);
        Assert.Equal("postgres:///q/src", envelope.SourceAddress);
        Assert.Equal("postgres:///q/dst", envelope.DestinationAddress);
        Assert.Equal("postgres:///q/reply", envelope.ResponseAddress);
        Assert.Equal("postgres:///q/fault", envelope.FaultAddress);
        Assert.Equal("application/json", envelope.ContentType);
        Assert.Equal("TestMessage", envelope.MessageType);
    }

    private static PostgresMessageItem CreateMessageItem(
        object? headers = null,
        byte[]? body = null,
        DateTime? sentTime = null,
        int deliveryCount = 0)
    {
        var headersBytes = headers is not null
            ? Encoding.UTF8.GetBytes(JsonSerializer.Serialize(headers))
            : ReadOnlyMemory<byte>.Empty;

        return new PostgresMessageItem(
            TransportMessageId: Guid.NewGuid(),
            Body: body ?? [],
            Headers: headersBytes,
            QueueId: 1,
            SentTime: sentTime ?? DateTime.UtcNow,
            DeliveryCount: deliveryCount,
            MaxDeliveryCount: 10,
            ErrorReason: null);
    }
}
