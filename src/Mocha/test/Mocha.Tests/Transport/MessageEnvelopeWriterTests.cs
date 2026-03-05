using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Mocha.Middlewares;

namespace Mocha.Tests;

public class MessageEnvelopeWriterTests
{
    [Fact]
    public void WriteMessage_Should_ProduceValidJson_When_AllFieldsPopulated()
    {
        // arrange
        var envelope = new MessageEnvelope
        {
            MessageId = "msg-001",
            CorrelationId = "corr-001",
            ConversationId = "conv-001",
            CausationId = "cause-001",
            SourceAddress = "source://test",
            DestinationAddress = "dest://test",
            ResponseAddress = "reply://test",
            FaultAddress = "fault://test",
            ContentType = "application/json",
            MessageType = "urn:message:TestEvent",
            EnclosedMessageTypes = ImmutableArray.Create("urn:message:TestEvent", "urn:message:IEvent"),
            SentAt = new DateTimeOffset(2026, 1, 15, 10, 30, 0, TimeSpan.Zero),
            DeliverBy = new DateTimeOffset(2026, 6, 1, 23, 59, 59, TimeSpan.Zero),
            DeliveryCount = 1,
            Headers = new Headers(),
            Body = Encoding.UTF8.GetBytes("""{"orderId":"1"}""")
        };
        envelope.Headers!.Set("x-trace", "abc123");

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // act
        var envelopeWriter = new MessageEnvelopeWriter(writer);
        envelopeWriter.WriteMessage(envelope);
        writer.Flush();

        // assert
        var json = Encoding.UTF8.GetString(stream.ToArray());
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("msg-001", root.GetProperty("messageId").GetString());
        Assert.Equal("corr-001", root.GetProperty("correlationId").GetString());
        Assert.Equal("conv-001", root.GetProperty("conversationId").GetString());
        Assert.Equal("cause-001", root.GetProperty("causationId").GetString());
        Assert.Equal("source://test", root.GetProperty("sourceAddress").GetString());
        Assert.Equal("dest://test", root.GetProperty("destinationAddress").GetString());
        Assert.Equal("reply://test", root.GetProperty("responseAddress").GetString());
        Assert.Equal("fault://test", root.GetProperty("faultAddress").GetString());
        Assert.Equal("application/json", root.GetProperty("contentType").GetString());
        Assert.Equal("urn:message:TestEvent", root.GetProperty("messageType").GetString());
        Assert.Equal(1, root.GetProperty("deliveryCount").GetInt32());
        Assert.True(root.TryGetProperty("sentAt", out _));
        Assert.True(root.TryGetProperty("deliverBy", out _));
        Assert.True(root.TryGetProperty("enclosedMessageTypes", out var types));
        Assert.Equal(2, types.GetArrayLength());
        Assert.True(root.TryGetProperty("headers", out var headers));
        Assert.Equal("abc123", headers.GetProperty("x-trace").GetString());
        Assert.True(root.TryGetProperty("body", out var body));
    }

    [Fact]
    public void WriteMessage_Should_OmitNullFields_When_OptionalFieldsAreNull()
    {
        // arrange
        var envelope = new MessageEnvelope { MessageId = "msg-002", MessageType = "urn:message:TestEvent" };

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // act
        var envelopeWriter = new MessageEnvelopeWriter(writer);
        envelopeWriter.WriteMessage(envelope);
        writer.Flush();

        // assert
        var json = Encoding.UTF8.GetString(stream.ToArray());
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("msg-002", root.GetProperty("messageId").GetString());
        Assert.Equal("urn:message:TestEvent", root.GetProperty("messageType").GetString());

        // Optional fields should NOT be present
        Assert.False(root.TryGetProperty("correlationId", out _));
        Assert.False(root.TryGetProperty("conversationId", out _));
        Assert.False(root.TryGetProperty("causationId", out _));
        Assert.False(root.TryGetProperty("responseAddress", out _));
        Assert.False(root.TryGetProperty("faultAddress", out _));
        Assert.False(root.TryGetProperty("headers", out _));
        Assert.False(root.TryGetProperty("sentAt", out _));
        Assert.False(root.TryGetProperty("deliverBy", out _));
        Assert.False(root.TryGetProperty("deliveryCount", out _));
        Assert.False(root.TryGetProperty("enclosedMessageTypes", out _));
        Assert.False(root.TryGetProperty("body", out _));
    }

    [Fact]
    public void WriteMessage_Should_ProduceEmptyObject_When_AllFieldsAreNull()
    {
        // arrange
        var envelope = new MessageEnvelope();

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // act
        var envelopeWriter = new MessageEnvelopeWriter(writer);
        envelopeWriter.WriteMessage(envelope);
        writer.Flush();

        // assert
        var json = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("{}", json);
    }

    [Fact]
    public void WriteMessage_Should_SerializeHeaders_When_HeadersArePresent()
    {
        // arrange
        var envelope = new MessageEnvelope { MessageId = "msg-003", Headers = new Headers() };
        envelope.Headers.Set("custom-key", "custom-value");
        envelope.Headers.Set("trace-id", "trace-123");

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // act
        var envelopeWriter = new MessageEnvelopeWriter(writer);
        envelopeWriter.WriteMessage(envelope);
        writer.Flush();

        // assert
        var json = Encoding.UTF8.GetString(stream.ToArray());
        using var doc = JsonDocument.Parse(json);
        var headers = doc.RootElement.GetProperty("headers");

        Assert.Equal("custom-value", headers.GetProperty("custom-key").GetString());
        Assert.Equal("trace-123", headers.GetProperty("trace-id").GetString());
    }

    [Fact]
    public void WriteMessage_Should_SerializeEmptyHeaders_When_HeadersCollectionIsEmpty()
    {
        // arrange
        var envelope = new MessageEnvelope { MessageId = "msg-004", Headers = new Headers() };

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // act
        var envelopeWriter = new MessageEnvelopeWriter(writer);
        envelopeWriter.WriteMessage(envelope);
        writer.Flush();

        // assert
        var json = Encoding.UTF8.GetString(stream.ToArray());
        using var doc = JsonDocument.Parse(json);
        var headers = doc.RootElement.GetProperty("headers");

        Assert.Equal(JsonValueKind.Object, headers.ValueKind);
        Assert.Empty(headers.EnumerateObject());
    }

    [Fact]
    public void WriteMessage_Should_SerializeBodyAsRawJson_When_BodyIsJson()
    {
        // arrange
        const string bodyJson = """{"orderId":"1","amount":100}""";
        var envelope = new MessageEnvelope { MessageId = "msg-005", Body = Encoding.UTF8.GetBytes(bodyJson) };

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // act
        var envelopeWriter = new MessageEnvelopeWriter(writer);
        envelopeWriter.WriteMessage(envelope);
        writer.Flush();

        // assert
        var json = Encoding.UTF8.GetString(stream.ToArray());
        using var doc = JsonDocument.Parse(json);
        var body = doc.RootElement.GetProperty("body");

        Assert.Equal(JsonValueKind.Object, body.ValueKind);
        Assert.Equal("1", body.GetProperty("orderId").GetString());
        Assert.Equal(100, body.GetProperty("amount").GetInt32());
    }

    [Fact]
    public void WriteMessage_Should_OmitBody_When_BodyIsEmpty()
    {
        // arrange
        var envelope = new MessageEnvelope { MessageId = "msg-006", Body = Array.Empty<byte>() };

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // act
        var envelopeWriter = new MessageEnvelopeWriter(writer);
        envelopeWriter.WriteMessage(envelope);
        writer.Flush();

        // assert
        var json = Encoding.UTF8.GetString(stream.ToArray());
        using var doc = JsonDocument.Parse(json);

        Assert.False(doc.RootElement.TryGetProperty("body", out _));
    }

    [Fact]
    public void WriteMessage_Should_SerializeBodyAsRawJson_When_BodyIsJsonArray()
    {
        // arrange
        const string bodyJson = """[1,2,3]""";
        var envelope = new MessageEnvelope { MessageId = "msg-007", Body = Encoding.UTF8.GetBytes(bodyJson) };

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // act
        var envelopeWriter = new MessageEnvelopeWriter(writer);
        envelopeWriter.WriteMessage(envelope);
        writer.Flush();

        // assert
        var json = Encoding.UTF8.GetString(stream.ToArray());
        using var doc = JsonDocument.Parse(json);
        var body = doc.RootElement.GetProperty("body");

        Assert.Equal(JsonValueKind.Array, body.ValueKind);
        Assert.Equal(3, body.GetArrayLength());
    }

    [Fact]
    public void WriteMessage_Should_SerializeDateTimeInIso8601_When_SentAtIsPresent()
    {
        // arrange
        var envelope = new MessageEnvelope
        {
            MessageId = "msg-008",
            SentAt = new DateTimeOffset(2026, 1, 15, 10, 30, 0, TimeSpan.Zero)
        };

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // act
        var envelopeWriter = new MessageEnvelopeWriter(writer);
        envelopeWriter.WriteMessage(envelope);
        writer.Flush();

        // assert
        var json = Encoding.UTF8.GetString(stream.ToArray());
        using var doc = JsonDocument.Parse(json);
        var sentAt = doc.RootElement.GetProperty("sentAt").GetString();

        Assert.NotNull(sentAt);
        Assert.Contains("2026-01-15", sentAt);
        Assert.Contains("10:30:00", sentAt);
    }

    [Fact]
    public void WriteMessage_Should_SerializeDateTimeInIso8601_When_DeliverByIsPresent()
    {
        // arrange
        var envelope = new MessageEnvelope
        {
            MessageId = "msg-009",
            DeliverBy = new DateTimeOffset(2026, 6, 1, 23, 59, 59, TimeSpan.Zero)
        };

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // act
        var envelopeWriter = new MessageEnvelopeWriter(writer);
        envelopeWriter.WriteMessage(envelope);
        writer.Flush();

        // assert
        var json = Encoding.UTF8.GetString(stream.ToArray());
        using var doc = JsonDocument.Parse(json);
        var deliverBy = doc.RootElement.GetProperty("deliverBy").GetString();

        Assert.NotNull(deliverBy);
        Assert.Contains("2026-06-01", deliverBy);
    }

    [Fact]
    public void WriteMessage_Should_SerializeEmptyArray_When_EnclosedMessageTypesIsEmpty()
    {
        // arrange
        var envelope = new MessageEnvelope
        {
            MessageId = "msg-010",
            EnclosedMessageTypes = ImmutableArray<string>.Empty
        };

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // act
        var envelopeWriter = new MessageEnvelopeWriter(writer);
        envelopeWriter.WriteMessage(envelope);
        writer.Flush();

        // assert
        var json = Encoding.UTF8.GetString(stream.ToArray());
        using var doc = JsonDocument.Parse(json);
        var types = doc.RootElement.GetProperty("enclosedMessageTypes");

        Assert.Equal(JsonValueKind.Array, types.ValueKind);
        Assert.Equal(0, types.GetArrayLength());
    }

    [Fact]
    public void WriteMessage_Should_SerializeSingleItem_When_EnclosedMessageTypesHasOneElement()
    {
        // arrange
        var envelope = new MessageEnvelope
        {
            MessageId = "msg-011",
            EnclosedMessageTypes = ImmutableArray.Create("urn:message:Foo")
        };

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // act
        var envelopeWriter = new MessageEnvelopeWriter(writer);
        envelopeWriter.WriteMessage(envelope);
        writer.Flush();

        // assert
        var json = Encoding.UTF8.GetString(stream.ToArray());
        using var doc = JsonDocument.Parse(json);
        var types = doc.RootElement.GetProperty("enclosedMessageTypes");

        Assert.Equal(1, types.GetArrayLength());
        Assert.Equal("urn:message:Foo", types[0].GetString());
    }

    [Fact]
    public void WriteMessage_Should_SerializeMultipleItems_When_EnclosedMessageTypesHasMultipleElements()
    {
        // arrange
        var envelope = new MessageEnvelope
        {
            MessageId = "msg-012",
            EnclosedMessageTypes = ImmutableArray.Create(
                "urn:message:TestEvent",
                "urn:message:IEvent",
                "urn:message:IDomainEvent")
        };

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // act
        var envelopeWriter = new MessageEnvelopeWriter(writer);
        envelopeWriter.WriteMessage(envelope);
        writer.Flush();

        // assert
        var json = Encoding.UTF8.GetString(stream.ToArray());
        using var doc = JsonDocument.Parse(json);
        var types = doc.RootElement.GetProperty("enclosedMessageTypes");

        Assert.Equal(3, types.GetArrayLength());
        Assert.Equal("urn:message:TestEvent", types[0].GetString());
        Assert.Equal("urn:message:IEvent", types[1].GetString());
        Assert.Equal("urn:message:IDomainEvent", types[2].GetString());
    }

    [Fact]
    public void WriteMessage_Should_SerializeDeliveryCount_When_DeliveryCountIsPresent()
    {
        // arrange
        var envelope = new MessageEnvelope { MessageId = "msg-013", DeliveryCount = 5 };

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // act
        var envelopeWriter = new MessageEnvelopeWriter(writer);
        envelopeWriter.WriteMessage(envelope);
        writer.Flush();

        // assert
        var json = Encoding.UTF8.GetString(stream.ToArray());
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(5, doc.RootElement.GetProperty("deliveryCount").GetInt32());
    }

    [Fact]
    public void WriteMessage_Should_SerializeZeroDeliveryCount_When_DeliveryCountIsZero()
    {
        // arrange
        var envelope = new MessageEnvelope { MessageId = "msg-014", DeliveryCount = 0 };

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // act
        var envelopeWriter = new MessageEnvelopeWriter(writer);
        envelopeWriter.WriteMessage(envelope);
        writer.Flush();

        // assert
        var json = Encoding.UTF8.GetString(stream.ToArray());
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(0, doc.RootElement.GetProperty("deliveryCount").GetInt32());
    }

    [Fact]
    public void Roundtrip_Should_PreserveData_When_WritingAndReadingFullEnvelope()
    {
        // arrange
        var original = new MessageEnvelope
        {
            MessageId = "msg-015",
            CorrelationId = "corr-015",
            ConversationId = "conv-015",
            CausationId = "cause-015",
            SourceAddress = "source://test",
            DestinationAddress = "dest://test",
            ResponseAddress = "reply://test",
            FaultAddress = "fault://test",
            ContentType = "application/json",
            MessageType = "urn:message:TestEvent",
            SentAt = new DateTimeOffset(2026, 1, 15, 10, 30, 0, TimeSpan.Zero),
            DeliverBy = new DateTimeOffset(2026, 6, 1, 23, 59, 59, TimeSpan.Zero),
            DeliveryCount = 3,
            EnclosedMessageTypes = ImmutableArray.Create("urn:message:TestEvent", "urn:message:IEvent"),
            Headers = new Headers(),
            Body = Encoding.UTF8.GetBytes("""{"orderId":"1"}""")
        };
        original.Headers!.Set("x-trace", "abc123");

        // act - write
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        new MessageEnvelopeWriter(writer).WriteMessage(original);
        writer.Flush();

        // act - read
        var bytes = stream.ToArray();
        var result = MessageEnvelopeReader.Parse(bytes);

        // assert
        Assert.Equal(original.MessageId, result.MessageId);
        Assert.Equal(original.CorrelationId, result.CorrelationId);
        Assert.Equal(original.ConversationId, result.ConversationId);
        Assert.Equal(original.CausationId, result.CausationId);
        Assert.Equal(original.SourceAddress, result.SourceAddress);
        Assert.Equal(original.DestinationAddress, result.DestinationAddress);
        Assert.Equal(original.ResponseAddress, result.ResponseAddress);
        Assert.Equal(original.FaultAddress, result.FaultAddress);
        Assert.Equal(original.ContentType, result.ContentType);
        Assert.Equal(original.MessageType, result.MessageType);
        Assert.Equal(original.SentAt, result.SentAt);
        Assert.Equal(original.DeliverBy, result.DeliverBy);
        Assert.Equal(original.DeliveryCount, result.DeliveryCount);
        Assert.NotNull(result.EnclosedMessageTypes);
        Assert.Equal(2, result.EnclosedMessageTypes!.Value.Length);
        Assert.NotNull(result.Headers);
        Assert.True(result.Headers!.TryGetValue("x-trace", out var traceValue));
        Assert.Equal("abc123", traceValue);
        Assert.False(result.Body.IsEmpty);
    }

    [Fact]
    public void Roundtrip_Should_PreserveData_When_WritingAndReadingMinimalEnvelope()
    {
        // arrange
        var original = new MessageEnvelope { MessageId = "msg-016", MessageType = "urn:message:TestEvent" };

        // act - write
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        new MessageEnvelopeWriter(writer).WriteMessage(original);
        writer.Flush();

        // act - read
        var bytes = stream.ToArray();
        var result = MessageEnvelopeReader.Parse(bytes);

        // assert
        Assert.Equal(original.MessageId, result.MessageId);
        Assert.Equal(original.MessageType, result.MessageType);
        Assert.Null(result.CorrelationId);
        Assert.Null(result.ResponseAddress);
    }

    [Fact]
    public void Roundtrip_Should_PreserveEmptyArray_When_EnclosedMessageTypesIsEmpty()
    {
        // arrange
        var original = new MessageEnvelope
        {
            MessageId = "msg-017",
            EnclosedMessageTypes = ImmutableArray<string>.Empty
        };

        // act - write
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        new MessageEnvelopeWriter(writer).WriteMessage(original);
        writer.Flush();

        // act - read
        var bytes = stream.ToArray();
        var result = MessageEnvelopeReader.Parse(bytes);

        // assert
        Assert.NotNull(result.EnclosedMessageTypes);
        Assert.Empty(result.EnclosedMessageTypes!.Value);
    }

    [Fact]
    public void WriteMessage_Should_SerializeAllAddresses_When_AllAddressFieldsArePresent()
    {
        // arrange
        var envelope = new MessageEnvelope
        {
            MessageId = "msg-018",
            SourceAddress = "source://app1",
            DestinationAddress = "dest://app2",
            ResponseAddress = "reply://app1",
            FaultAddress = "fault://error-queue"
        };

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // act
        var envelopeWriter = new MessageEnvelopeWriter(writer);
        envelopeWriter.WriteMessage(envelope);
        writer.Flush();

        // assert
        var json = Encoding.UTF8.GetString(stream.ToArray());
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("source://app1", root.GetProperty("sourceAddress").GetString());
        Assert.Equal("dest://app2", root.GetProperty("destinationAddress").GetString());
        Assert.Equal("reply://app1", root.GetProperty("responseAddress").GetString());
        Assert.Equal("fault://error-queue", root.GetProperty("faultAddress").GetString());
    }

    [Fact]
    public void WriteMessage_Should_SerializeContentType_When_ContentTypeIsPresent()
    {
        // arrange
        var envelope = new MessageEnvelope { MessageId = "msg-019", ContentType = "application/xml" };

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // act
        var envelopeWriter = new MessageEnvelopeWriter(writer);
        envelopeWriter.WriteMessage(envelope);
        writer.Flush();

        // assert
        var json = Encoding.UTF8.GetString(stream.ToArray());
        using var doc = JsonDocument.Parse(json);

        Assert.Equal("application/xml", doc.RootElement.GetProperty("contentType").GetString());
    }

    [Fact]
    public void WriteMessage_Should_SerializeMessageType_When_MessageTypeIsPresent()
    {
        // arrange
        var envelope = new MessageEnvelope { MessageId = "msg-020", MessageType = "urn:message:OrderCreated" };

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // act
        var envelopeWriter = new MessageEnvelopeWriter(writer);
        envelopeWriter.WriteMessage(envelope);
        writer.Flush();

        // assert
        var json = Encoding.UTF8.GetString(stream.ToArray());
        using var doc = JsonDocument.Parse(json);

        Assert.Equal("urn:message:OrderCreated", doc.RootElement.GetProperty("messageType").GetString());
    }
}
