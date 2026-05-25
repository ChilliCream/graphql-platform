using System.Text;
using System.Text.Json;
using Mocha.Middlewares;

namespace Mocha.Tests;

public class MessageEnvelopeReaderTests
{
    private static ReadOnlyMemory<byte> ToBytes(string json) => Encoding.UTF8.GetBytes(json);

    [Fact]
    public void Parse_Should_ReturnFullyPopulatedEnvelope_When_AllFieldsPresent()
    {
        // arrange - body must be last because the reader uses Skip() to capture raw JSON bytes
        const string json = """
            {
                "messageId": "msg-001",
                "correlationId": "corr-001",
                "conversationId": "conv-001",
                "causationId": "cause-001",
                "sourceAddress": "source://test",
                "destinationAddress": "dest://test",
                "responseAddress": "reply://test",
                "faultAddress": "fault://test",
                "contentType": "application/json",
                "messageType": "urn:message:TestEvent",
                "sentAt": "2026-01-15T10:30:00Z",
                "deliverBy": "2026-06-01T23:59:59Z",
                "deliveryCount": 1,
                "enclosedMessageTypes": ["urn:message:TestEvent", "urn:message:IEvent"],
                "headers": {"x-trace": "abc123"},
                "body": {"orderId":"1"}
            }
            """;

        // act
        var envelope = MessageEnvelopeReader.Parse(ToBytes(json));

        // assert
        Assert.Equal("msg-001", envelope.MessageId);
        Assert.Equal("corr-001", envelope.CorrelationId);
        Assert.Equal("conv-001", envelope.ConversationId);
        Assert.Equal("cause-001", envelope.CausationId);
        Assert.Equal("source://test", envelope.SourceAddress);
        Assert.Equal("dest://test", envelope.DestinationAddress);
        Assert.Equal("reply://test", envelope.ResponseAddress);
        Assert.Equal("fault://test", envelope.FaultAddress);
        Assert.Equal("application/json", envelope.ContentType);
        Assert.Equal("urn:message:TestEvent", envelope.MessageType);
        Assert.NotNull(envelope.SentAt);
        Assert.NotNull(envelope.DeliverBy);
        Assert.Equal(1, envelope.DeliveryCount);
        Assert.NotNull(envelope.EnclosedMessageTypes);
        Assert.Equal(2, envelope.EnclosedMessageTypes!.Value.Length);
        Assert.Equal("urn:message:TestEvent", envelope.EnclosedMessageTypes!.Value[0]);
        Assert.Equal("urn:message:IEvent", envelope.EnclosedMessageTypes!.Value[1]);
        Assert.NotNull(envelope.Headers);
        Assert.False(envelope.Body.IsEmpty);
    }

    [Fact]
    public void Parse_Should_ReturnEnvelopeWithOnlyMessageIdAndType_When_MinimalEnvelope()
    {
        // arrange
        const string json = """
            {
                "messageId": "msg-002",
                "messageType": "urn:message:TestEvent"
            }
            """;

        // act
        var envelope = MessageEnvelopeReader.Parse(ToBytes(json));

        // assert
        Assert.Equal("msg-002", envelope.MessageId);
        Assert.Equal("urn:message:TestEvent", envelope.MessageType);
        Assert.Null(envelope.CorrelationId);
        Assert.Null(envelope.ResponseAddress);
        // DeliveryCount defaults to 0 (int field) when not present in JSON
        Assert.Equal(0, envelope.DeliveryCount);
        Assert.Null(envelope.EnclosedMessageTypes);
    }

    [Fact]
    public void Parse_Should_ReturnEnvelopeWithNulls_When_EmptyObject()
    {
        // arrange
        const string json = "{}";

        // act
        var envelope = MessageEnvelopeReader.Parse(ToBytes(json));

        // assert
        Assert.Null(envelope.MessageId);
        Assert.Null(envelope.MessageType);
    }

    [Fact]
    public void Parse_Should_ReturnEmptyArray_When_EnclosedMessageTypesIsEmpty()
    {
        const string json = """{"enclosedMessageTypes": []}""";

        var envelope = MessageEnvelopeReader.Parse(ToBytes(json));

        Assert.NotNull(envelope.EnclosedMessageTypes);
        Assert.Empty(envelope.EnclosedMessageTypes!.Value);
    }

    [Fact]
    public void Parse_Should_ReturnSingleItem_When_SingleEnclosedMessageType()
    {
        const string json = """{"enclosedMessageTypes": ["urn:message:Foo"]}""";

        var envelope = MessageEnvelopeReader.Parse(ToBytes(json));

        Assert.NotNull(envelope.EnclosedMessageTypes);
        Assert.Single(envelope.EnclosedMessageTypes!.Value);
        Assert.Equal("urn:message:Foo", envelope.EnclosedMessageTypes!.Value[0]);
    }

    [Fact]
    public void Parse_Should_CaptureRawJsonBytes_When_BodyIsJson()
    {
        // The body is captured as raw JSON bytes (not base64).
        // The reader slices the original buffer around the JSON value.
        const string json = """{"body": {"orderId":"1"}}""";

        var envelope = MessageEnvelopeReader.Parse(ToBytes(json));

        var bodyString = Encoding.UTF8.GetString(envelope.Body.Span);
        Assert.Contains("orderId", bodyString);
    }

    [Fact]
    public void Parse_Should_ReturnEmptyMemory_When_BodyIsEmpty()
    {
        const string json = """{"messageId": "msg-003"}""";

        var envelope = MessageEnvelopeReader.Parse(ToBytes(json));

        Assert.True(envelope.Body.IsEmpty);
    }

    [Fact]
    public void Parse_Should_ParseIso8601DateTime_When_SentAtIsPresent()
    {
        const string json = """{"sentAt": "2026-01-15T10:30:00+00:00"}""";

        var envelope = MessageEnvelopeReader.Parse(ToBytes(json));

        Assert.NotNull(envelope.SentAt);
        Assert.Equal(2026, envelope.SentAt!.Value.Year);
        Assert.Equal(1, envelope.SentAt!.Value.Month);
        Assert.Equal(15, envelope.SentAt!.Value.Day);
    }

    [Fact]
    public void Parse_Should_ParseIso8601DateTime_When_DeliverByIsPresent()
    {
        const string json = """{"deliverBy": "2026-06-01T23:59:59Z"}""";

        var envelope = MessageEnvelopeReader.Parse(ToBytes(json));

        Assert.NotNull(envelope.DeliverBy);
        Assert.Equal(2026, envelope.DeliverBy!.Value.Year);
        Assert.Equal(6, envelope.DeliverBy!.Value.Month);
        Assert.Equal(1, envelope.DeliverBy!.Value.Day);
    }

    [Fact]
    public void Parse_Should_ReturnNullFields_When_StringFieldsAreNull()
    {
        const string json = """
            {
                "messageId": null,
                "correlationId": null,
                "responseAddress": null
            }
            """;

        var envelope = MessageEnvelopeReader.Parse(ToBytes(json));

        Assert.Null(envelope.MessageId);
        Assert.Null(envelope.CorrelationId);
        Assert.Null(envelope.ResponseAddress);
    }

    [Fact]
    public void Parse_Should_DeserializeHeaders_When_HeadersArePresent()
    {
        const string json = """
            {
                "headers": {
                    "custom-key": "custom-value"
                }
            }
            """;

        var envelope = MessageEnvelopeReader.Parse(ToBytes(json));

        Assert.NotNull(envelope.Headers);
        Assert.True(envelope.Headers!.TryGetValue("custom-key", out var value));
        Assert.Equal("custom-value", value);
    }

    [Fact]
    public void Parse_Should_ThrowJsonException_When_JsonIsMalformed()
    {
        // JsonReaderException (subclass of JsonException) is thrown for malformed input
        const string json = "{ invalid json }";
        Assert.ThrowsAny<JsonException>(() => MessageEnvelopeReader.Parse(ToBytes(json)));
    }

    [Fact]
    public void Parse_Should_ThrowJsonException_When_InputIsEmpty()
    {
        // JsonReaderException (subclass of JsonException) is thrown for empty input
        Assert.ThrowsAny<JsonException>(() => MessageEnvelopeReader.Parse(ReadOnlyMemory<byte>.Empty));
    }

    [Fact]
    public void Parse_Should_ThrowJsonException_When_UnknownPropertyPresent()
    {
        const string json = """{"unknownField": "value"}""";
        Assert.Throws<JsonException>(() => MessageEnvelopeReader.Parse(ToBytes(json)));
    }

    [Fact]
    public void Parse_Should_ParseIso8601DateTime_When_ScheduledTimeIsPresent()
    {
        const string json = """{"scheduledTime": "2026-06-01T12:00:00+00:00"}""";

        var envelope = MessageEnvelopeReader.Parse(ToBytes(json));

        Assert.NotNull(envelope.ScheduledTime);
        Assert.Equal(2026, envelope.ScheduledTime!.Value.Year);
        Assert.Equal(6, envelope.ScheduledTime!.Value.Month);
        Assert.Equal(1, envelope.ScheduledTime!.Value.Day);
    }

    [Fact]
    public void Parse_Should_ReturnNullScheduledTime_When_ScheduledTimeAbsent()
    {
        const string json = """{"messageId": "msg-100"}""";

        var envelope = MessageEnvelopeReader.Parse(ToBytes(json));

        Assert.Null(envelope.ScheduledTime);
    }
}
