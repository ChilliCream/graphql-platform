using System.Text;
using Confluent.Kafka;
using KafkaHeaders = Confluent.Kafka.Headers;

namespace Mocha.Transport.Kafka.Tests;

public class KafkaMessageEnvelopeParserTests
{
    private readonly KafkaMessageEnvelopeParser _parser = KafkaMessageEnvelopeParser.Instance;

    [Fact]
    public void Parse_Should_ExtractMessageId_When_HeaderPresent()
    {
        // arrange
        var headers = new KafkaHeaders();
        headers.Add(KafkaMessageHeaders.MessageId, Encoding.UTF8.GetBytes("msg-123"));
        var result = CreateConsumeResult(headers: headers);

        // act
        var envelope = _parser.Parse(result);

        // assert
        Assert.Equal("msg-123", envelope.MessageId);
    }

    [Fact]
    public void Parse_Should_ExtractCorrelationId_When_HeaderPresent()
    {
        // arrange
        var headers = new KafkaHeaders();
        headers.Add(KafkaMessageHeaders.CorrelationId, Encoding.UTF8.GetBytes("corr-456"));
        var result = CreateConsumeResult(headers: headers);

        // act
        var envelope = _parser.Parse(result);

        // assert
        Assert.Equal("corr-456", envelope.CorrelationId);
    }

    [Fact]
    public void Parse_Should_ExtractConversationId_When_HeaderPresent()
    {
        // arrange
        var headers = new KafkaHeaders();
        headers.Add(KafkaMessageHeaders.ConversationId, Encoding.UTF8.GetBytes("conv-789"));
        var result = CreateConsumeResult(headers: headers);

        // act
        var envelope = _parser.Parse(result);

        // assert
        Assert.Equal("conv-789", envelope.ConversationId);
    }

    [Fact]
    public void Parse_Should_ExtractCausationId_When_HeaderPresent()
    {
        // arrange
        var headers = new KafkaHeaders();
        headers.Add(KafkaMessageHeaders.CausationId, Encoding.UTF8.GetBytes("cause-101"));
        var result = CreateConsumeResult(headers: headers);

        // act
        var envelope = _parser.Parse(result);

        // assert
        Assert.Equal("cause-101", envelope.CausationId);
    }

    [Fact]
    public void Parse_Should_ExtractSourceAddress_When_HeaderPresent()
    {
        // arrange
        var headers = new KafkaHeaders();
        headers.Add(KafkaMessageHeaders.SourceAddress, Encoding.UTF8.GetBytes("kafka:///t/source"));
        var result = CreateConsumeResult(headers: headers);

        // act
        var envelope = _parser.Parse(result);

        // assert
        Assert.Equal("kafka:///t/source", envelope.SourceAddress);
    }

    [Fact]
    public void Parse_Should_ExtractDestinationAddress_When_HeaderPresent()
    {
        // arrange
        var headers = new KafkaHeaders();
        headers.Add(KafkaMessageHeaders.DestinationAddress, Encoding.UTF8.GetBytes("kafka:///t/destination"));
        var result = CreateConsumeResult(headers: headers);

        // act
        var envelope = _parser.Parse(result);

        // assert
        Assert.Equal("kafka:///t/destination", envelope.DestinationAddress);
    }

    [Fact]
    public void Parse_Should_ExtractResponseAddress_When_HeaderPresent()
    {
        // arrange
        var headers = new KafkaHeaders();
        headers.Add(KafkaMessageHeaders.ResponseAddress, Encoding.UTF8.GetBytes("kafka:///t/reply"));
        var result = CreateConsumeResult(headers: headers);

        // act
        var envelope = _parser.Parse(result);

        // assert
        Assert.Equal("kafka:///t/reply", envelope.ResponseAddress);
    }

    [Fact]
    public void Parse_Should_ExtractFaultAddress_When_HeaderPresent()
    {
        // arrange
        var headers = new KafkaHeaders();
        headers.Add(KafkaMessageHeaders.FaultAddress, Encoding.UTF8.GetBytes("kafka:///t/fault"));
        var result = CreateConsumeResult(headers: headers);

        // act
        var envelope = _parser.Parse(result);

        // assert
        Assert.Equal("kafka:///t/fault", envelope.FaultAddress);
    }

    [Fact]
    public void Parse_Should_ExtractContentType_When_HeaderPresent()
    {
        // arrange
        var headers = new KafkaHeaders();
        headers.Add(KafkaMessageHeaders.ContentType, Encoding.UTF8.GetBytes("application/json"));
        var result = CreateConsumeResult(headers: headers);

        // act
        var envelope = _parser.Parse(result);

        // assert
        Assert.Equal("application/json", envelope.ContentType);
    }

    [Fact]
    public void Parse_Should_ExtractMessageType_When_HeaderPresent()
    {
        // arrange
        var headers = new KafkaHeaders();
        headers.Add(KafkaMessageHeaders.MessageType, Encoding.UTF8.GetBytes("OrderCreated"));
        var result = CreateConsumeResult(headers: headers);

        // act
        var envelope = _parser.Parse(result);

        // assert
        Assert.Equal("OrderCreated", envelope.MessageType);
    }

    [Fact]
    public void Parse_Should_ExtractBody_When_BodyPresent()
    {
        // arrange
        var body = "{\"orderId\":\"ORD-1\"}"u8.ToArray();
        var result = CreateConsumeResult(body: body);

        // act
        var envelope = _parser.Parse(result);

        // assert
        Assert.Equal(body, envelope.Body.ToArray());
    }

    [Fact]
    public void Parse_Should_ExtractSentAt_When_HeaderPresent()
    {
        // arrange
        var sentAt = new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero);
        var headers = new KafkaHeaders();
        headers.Add(KafkaMessageHeaders.SentAt, Encoding.UTF8.GetBytes(sentAt.ToString("O")));
        var result = CreateConsumeResult(headers: headers);

        // act
        var envelope = _parser.Parse(result);

        // assert
        Assert.NotNull(envelope.SentAt);
        Assert.Equal(sentAt, envelope.SentAt.Value);
    }

    [Fact]
    public void Parse_Should_ReturnNullSentAt_When_HeaderMissing()
    {
        // arrange
        var result = CreateConsumeResult();

        // act
        var envelope = _parser.Parse(result);

        // assert
        Assert.Null(envelope.SentAt);
    }

    [Fact]
    public void Parse_Should_ReturnEmptyBody_When_ValueNull()
    {
        // arrange
        var result = CreateConsumeResult(body: null);

        // act
        var envelope = _parser.Parse(result);

        // assert
        Assert.Empty(envelope.Body.ToArray());
    }

    [Fact]
    public void Parse_Should_SplitEnclosedMessageTypes_When_CommaSeparated()
    {
        // arrange
        var headers = new KafkaHeaders();
        headers.Add(KafkaMessageHeaders.EnclosedMessageTypes, Encoding.UTF8.GetBytes("OrderCreated, IEvent, IMessage"));
        var result = CreateConsumeResult(headers: headers);

        // act
        var envelope = _parser.Parse(result);

        // assert
        Assert.NotNull(envelope.EnclosedMessageTypes);
        Assert.Equal(3, envelope.EnclosedMessageTypes.Value.Length);
        Assert.Equal("OrderCreated", envelope.EnclosedMessageTypes.Value[0]);
        Assert.Equal("IEvent", envelope.EnclosedMessageTypes.Value[1]);
        Assert.Equal("IMessage", envelope.EnclosedMessageTypes.Value[2]);
    }

    [Fact]
    public void Parse_Should_ReturnNullEnclosedMessageTypes_When_HeaderMissing()
    {
        // arrange
        var result = CreateConsumeResult();

        // act
        var envelope = _parser.Parse(result);

        // assert
        Assert.Null(envelope.EnclosedMessageTypes);
    }

    [Fact]
    public void Parse_Should_ReturnNullEnclosedMessageTypes_When_HeaderEmpty()
    {
        // arrange
        var headers = new KafkaHeaders();
        headers.Add(KafkaMessageHeaders.EnclosedMessageTypes, Encoding.UTF8.GetBytes(""));
        var result = CreateConsumeResult(headers: headers);

        // act
        var envelope = _parser.Parse(result);

        // assert
        Assert.Null(envelope.EnclosedMessageTypes);
    }

    [Fact]
    public void Parse_Should_ExtractCustomHeaders_When_Present()
    {
        // arrange
        var headers = new KafkaHeaders();
        headers.Add(KafkaMessageHeaders.MessageId, Encoding.UTF8.GetBytes("msg-1"));
        headers.Add("x-custom-header", Encoding.UTF8.GetBytes("custom-value"));
        var result = CreateConsumeResult(headers: headers);

        // act
        var envelope = _parser.Parse(result);

        // assert
        Assert.True(envelope.Headers!.TryGetValue("x-custom-header", out var value));
        Assert.Equal("custom-value", value);
    }

    [Fact]
    public void Parse_Should_ExcludeWellKnownHeaders_When_BuildingCustomHeaders()
    {
        // arrange
        var headers = new KafkaHeaders();
        headers.Add(KafkaMessageHeaders.MessageId, Encoding.UTF8.GetBytes("msg-1"));
        headers.Add(KafkaMessageHeaders.CorrelationId, Encoding.UTF8.GetBytes("corr-1"));
        headers.Add("x-custom", Encoding.UTF8.GetBytes("value"));
        var result = CreateConsumeResult(headers: headers);

        // act
        var envelope = _parser.Parse(result);

        // assert
        Assert.Equal(1, envelope.Headers!.Count);
        Assert.True(envelope.Headers.TryGetValue("x-custom", out var value));
        Assert.Equal("value", value);
    }

    [Fact]
    public void Parse_Should_ReturnEmptyHeaders_When_NoHeaders()
    {
        // arrange
        var result = CreateConsumeResult(headers: null);

        // act
        var envelope = _parser.Parse(result);

        // assert
        Assert.NotNull(envelope.Headers);
        Assert.Equal(0, envelope.Headers.Count);
    }

    [Fact]
    public void Parse_Should_ExtractAllStandardHeaders_When_AllPresent()
    {
        // arrange
        var headers = new KafkaHeaders();
        headers.Add(KafkaMessageHeaders.MessageId, Encoding.UTF8.GetBytes("msg-all"));
        headers.Add(KafkaMessageHeaders.CorrelationId, Encoding.UTF8.GetBytes("corr-all"));
        headers.Add(KafkaMessageHeaders.ConversationId, Encoding.UTF8.GetBytes("conv-all"));
        headers.Add(KafkaMessageHeaders.CausationId, Encoding.UTF8.GetBytes("cause-all"));
        headers.Add(KafkaMessageHeaders.SourceAddress, Encoding.UTF8.GetBytes("kafka:///t/src"));
        headers.Add(KafkaMessageHeaders.DestinationAddress, Encoding.UTF8.GetBytes("kafka:///t/dst"));
        headers.Add(KafkaMessageHeaders.ResponseAddress, Encoding.UTF8.GetBytes("kafka:///t/reply"));
        headers.Add(KafkaMessageHeaders.FaultAddress, Encoding.UTF8.GetBytes("kafka:///t/fault"));
        headers.Add(KafkaMessageHeaders.ContentType, Encoding.UTF8.GetBytes("application/json"));
        headers.Add(KafkaMessageHeaders.MessageType, Encoding.UTF8.GetBytes("TestMessage"));
        var result = CreateConsumeResult(headers: headers);

        // act
        var envelope = _parser.Parse(result);

        // assert
        Assert.Equal("msg-all", envelope.MessageId);
        Assert.Equal("corr-all", envelope.CorrelationId);
        Assert.Equal("conv-all", envelope.ConversationId);
        Assert.Equal("cause-all", envelope.CausationId);
        Assert.Equal("kafka:///t/src", envelope.SourceAddress);
        Assert.Equal("kafka:///t/dst", envelope.DestinationAddress);
        Assert.Equal("kafka:///t/reply", envelope.ResponseAddress);
        Assert.Equal("kafka:///t/fault", envelope.FaultAddress);
        Assert.Equal("application/json", envelope.ContentType);
        Assert.Equal("TestMessage", envelope.MessageType);
    }

    [Fact]
    public void Parse_Should_ReturnNullProperties_When_MinimalHeaders()
    {
        // arrange
        var result = CreateConsumeResult();

        // act
        var envelope = _parser.Parse(result);

        // assert
        Assert.Null(envelope.MessageId);
        Assert.Null(envelope.CorrelationId);
        Assert.Null(envelope.ConversationId);
        Assert.Null(envelope.CausationId);
        Assert.Null(envelope.SourceAddress);
        Assert.Null(envelope.DestinationAddress);
        Assert.Null(envelope.ResponseAddress);
        Assert.Null(envelope.FaultAddress);
        Assert.Null(envelope.ContentType);
        Assert.Null(envelope.MessageType);
    }

    private static ConsumeResult<byte[], byte[]> CreateConsumeResult(
        KafkaHeaders? headers = null,
        byte[]? body = null)
    {
        return new ConsumeResult<byte[], byte[]>
        {
            Message = new Message<byte[], byte[]>
            {
                Key = null!,
                Value = body!,
                Headers = headers ?? new KafkaHeaders()
            },
            Topic = "test-topic",
            Partition = new Partition(0),
            Offset = new Offset(0)
        };
    }
}
