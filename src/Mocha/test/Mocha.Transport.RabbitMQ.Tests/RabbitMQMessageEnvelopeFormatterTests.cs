using System.Collections;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using Mocha.Middlewares;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests;

public class RabbitMQMessageEnvelopeFormatterTests
{
    private static readonly DateTimeOffset s_now = new(2026, 8, 12, 10, 30, 0, TimeSpan.Zero);
    private static readonly TimeProvider s_timeProvider = new FixedTimeProvider(s_now);

    [Fact]
    public void Format_Should_SetBasicProperties_When_EnvelopeContainsMetadata()
    {
        // arrange
        var envelope = new MessageEnvelope
        {
            MessageId = "message-1",
            CorrelationId = "correlation-1",
            ResponseAddress = "reply-queue",
            ContentType = "application/json",
            MessageType = "OrderCreated"
        };

        // act
        var properties = RabbitMQMessageEnvelopeFormatter.Format(envelope, s_timeProvider);

        // assert
        Assert.Equal("message-1", properties.MessageId);
        Assert.Equal("correlation-1", properties.CorrelationId);
        Assert.Equal("reply-queue", properties.ReplyTo);
        Assert.Equal("application/json", properties.ContentType);
        Assert.Equal("OrderCreated", properties.Type);
        Assert.Equal(DeliveryModes.Persistent, properties.DeliveryMode);
        Assert.Equal(s_now.ToUnixTimeSeconds(), properties.Timestamp.UnixTime);
        Assert.Null(properties.Expiration);
    }

    [Fact]
    public void Format_Should_SetExpiration_When_EnvelopeHasDeliverBy()
    {
        // arrange
        var envelope = new MessageEnvelope { DeliverBy = s_now.AddSeconds(60) };

        // act
        var properties = RabbitMQMessageEnvelopeFormatter.Format(envelope, s_timeProvider);

        // assert
        Assert.Equal("60000", properties.Expiration);
    }

    [Fact]
    public void Format_Should_ConvertDateTimeOffset_When_HeaderContainsDateTimeOffset()
    {
        // arrange
        var dto = new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero);
        var envelope = new MessageEnvelope
        {
            Headers = new Headers([new HeaderValue { Key = "x-timestamp", Value = dto }])
        };

        // act
        var headers = RabbitMQMessageEnvelopeFormatter.Format(envelope, s_timeProvider).Headers!;

        // assert
        var timestamp = Assert.IsType<AmqpTimestamp>(headers["x-timestamp"]);
        Assert.Equal(dto.ToUnixTimeSeconds(), timestamp.UnixTime);
    }

    [Fact]
    public void Format_Should_ConvertDateTime_When_HeaderContainsDateTime()
    {
        // arrange
        var dt = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var envelope = new MessageEnvelope
        {
            Headers = new Headers([new HeaderValue { Key = "x-timestamp", Value = dt }])
        };

        // act
        var headers = RabbitMQMessageEnvelopeFormatter.Format(envelope, s_timeProvider).Headers!;

        // assert
        var timestamp = Assert.IsType<AmqpTimestamp>(headers["x-timestamp"]);
        var expected = new DateTimeOffset(dt).ToUnixTimeSeconds();
        Assert.Equal(expected, timestamp.UnixTime);
    }

    [Fact]
    public void Format_Should_ConvertNestedTimestamps_When_HeaderIsDeathTable()
    {
        // arrange
        // the x-death shape the receive side produces, whose nested date has no wire form
        var envelope = new MessageEnvelope
        {
            Headers = new Headers([
                new HeaderValue
                {
                    Key = "x-death",
                    Value = new object?[]
                    {
                        new Dictionary<string, object?>
                        {
                            ["queue"] = "product-queue",
                            ["time"] = DateTimeOffset.FromUnixTimeSeconds(1700000000)
                        }
                    }
                }
            ])
        };

        // act
        var headers = RabbitMQMessageEnvelopeFormatter.Format(envelope, s_timeProvider).Headers!;

        // assert
        Assert.Equal(
            new Dictionary<string, object?>
            {
                ["queue"] = "product-queue",
                ["time"] = new AmqpTimestamp(1700000000)
            },
            Assert.IsType<Dictionary<string, object?>>(
                Assert.Single(Assert.IsType<List<object?>>(headers["x-death"]))));
    }

    [Fact]
    public void Format_Should_ConvertToText_When_HeaderIsUnsignedLong()
    {
        // arrange
        // a field table has no unsigned 64 bit type, and a double would round off digits
        var envelope = new MessageEnvelope
        {
            Headers = new Headers([new HeaderValue { Key = "offset", Value = ulong.MaxValue }])
        };

        // act
        var headers = RabbitMQMessageEnvelopeFormatter.Format(envelope, s_timeProvider).Headers!;

        // assert
        Assert.Equal("18446744073709551615", headers["offset"]);
    }

    [Fact]
    public void Format_Should_DeclareBinary_When_HeaderIsABytePayload()
    {
        // arrange
        // a long string carries text and binary alike, so sending one leaves the receive side guessing
        var envelope = new MessageEnvelope
        {
            Headers = new Headers([
                new HeaderValue { Key = "x-signature", Value = "COM1"u8.ToArray() },
                new HeaderValue { Key = "x-segment", Value = new ArraySegment<byte>("COM2"u8.ToArray()) }
            ])
        };

        // act
        var headers = RabbitMQMessageEnvelopeFormatter.Format(envelope, s_timeProvider).Headers!;

        // assert
        Assert.Equal("COM1"u8.ToArray(), Assert.IsType<BinaryTableValue>(headers["x-signature"]).Bytes);
        Assert.Equal("COM2"u8.ToArray(), Assert.IsType<BinaryTableValue>(headers["x-segment"]).Bytes);
    }

    [Fact]
    public void Format_Should_KeepNullValues_When_HeaderValueIsNull()
    {
        // arrange
        var envelope = new MessageEnvelope
        {
            Headers = new Headers([new HeaderValue { Key = "x-null", Value = null }])
        };

        // act
        var headers = RabbitMQMessageEnvelopeFormatter.Format(envelope, s_timeProvider).Headers!;

        // assert
        Assert.True(headers.ContainsKey("x-null"));
        Assert.Null(headers["x-null"]);
    }

    [Fact]
    public void Format_Should_PassThroughOtherTypes_When_HeaderContainsString()
    {
        // arrange
        var envelope = new MessageEnvelope
        {
            Headers = new Headers([
                new HeaderValue { Key = "x-string", Value = "hello" },
                new HeaderValue { Key = "x-int", Value = 42 }
            ])
        };

        // act
        var headers = RabbitMQMessageEnvelopeFormatter.Format(envelope, s_timeProvider).Headers!;

        // assert
        Assert.Equal("hello", headers["x-string"]);
        Assert.Equal(42, headers["x-int"]);
    }

    [Fact]
    public void Format_Should_SetConversationId_When_EnvelopeHasConversationId()
    {
        // arrange
        var envelope = new MessageEnvelope { ConversationId = "conv-123" };

        // act
        var headers = RabbitMQMessageEnvelopeFormatter.Format(envelope, s_timeProvider).Headers!;

        // assert
        Assert.Equal("conv-123", headers[MessageHeaders.Transport.ConversationId.Key]);
    }

    [Fact]
    public void Format_Should_SetCausationId_When_EnvelopeHasCausationId()
    {
        // arrange
        var envelope = new MessageEnvelope { CausationId = "cause-456" };

        // act
        var headers = RabbitMQMessageEnvelopeFormatter.Format(envelope, s_timeProvider).Headers!;

        // assert
        Assert.Equal("cause-456", headers[MessageHeaders.Transport.CausationId.Key]);
    }

    [Fact]
    public void Format_Should_SetSourceAddress_When_EnvelopeHasSourceAddress()
    {
        // arrange
        var envelope = new MessageEnvelope { SourceAddress = "rabbitmq:///q/source-q" };

        // act
        var headers = RabbitMQMessageEnvelopeFormatter.Format(envelope, s_timeProvider).Headers!;

        // assert
        Assert.Equal("rabbitmq:///q/source-q", headers[MessageHeaders.Transport.SourceAddress.Key]);
    }

    [Fact]
    public void Format_Should_SetDestinationAddress_When_EnvelopeHasDestinationAddress()
    {
        // arrange
        var envelope = new MessageEnvelope { DestinationAddress = "rabbitmq:///q/dest-q" };

        // act
        var headers = RabbitMQMessageEnvelopeFormatter.Format(envelope, s_timeProvider).Headers!;

        // assert
        Assert.Equal("rabbitmq:///q/dest-q", headers[MessageHeaders.Transport.DestinationAddress.Key]);
    }

    [Fact]
    public void Format_Should_SetFaultAddress_When_EnvelopeHasFaultAddress()
    {
        // arrange
        var envelope = new MessageEnvelope { FaultAddress = "rabbitmq:///q/fault-q" };

        // act
        var headers = RabbitMQMessageEnvelopeFormatter.Format(envelope, s_timeProvider).Headers!;

        // assert
        Assert.Equal("rabbitmq:///q/fault-q", headers[MessageHeaders.Transport.FaultAddress.Key]);
    }

    [Fact]
    public void Format_Should_SetEnclosedMessageTypes_When_EnvelopeHasTypes()
    {
        // arrange
        var types = ImmutableArray.Create("urn:message:OrderCreated", "urn:message:IEvent");
        var envelope = new MessageEnvelope { EnclosedMessageTypes = types };

        // act
        var headers = RabbitMQMessageEnvelopeFormatter.Format(envelope, s_timeProvider).Headers!;

        // assert
        Assert.True(headers.ContainsKey(MessageHeaders.Transport.EnclosedMessageTypes.Key));
        Assert.Equal(types, headers[MessageHeaders.Transport.EnclosedMessageTypes.Key]);
    }

    [Fact]
    public void Format_Should_SetMessageType_When_EnvelopeHasMessageType()
    {
        // arrange
        var envelope = new MessageEnvelope { MessageType = "urn:message:OrderCreated" };

        // act
        var headers = RabbitMQMessageEnvelopeFormatter.Format(envelope, s_timeProvider).Headers!;

        // assert
        Assert.Equal("urn:message:OrderCreated", headers[MessageHeaders.Transport.MessageType.Key]);
    }

    [Fact]
    public void Format_Should_ReturnEmptyHeaders_When_EnvelopeIsEmpty()
    {
        // arrange
        var envelope = new MessageEnvelope();

        // act
        var headers = RabbitMQMessageEnvelopeFormatter.Format(envelope, s_timeProvider).Headers!;

        // assert
        Assert.Empty(headers);
    }

    [Fact]
    public void Format_Should_MapToTableValues_When_TypesHaveNoWireForm()
    {
        // arrange
        // every type a field table cannot represent
        using var document = JsonDocument.Parse("""{"k":1}""");
        var envelope = new MessageEnvelope
        {
            Headers = new Headers(
            [
                new HeaderValue { Key = "char", Value = 'c' },
                new HeaderValue { Key = "guid", Value = Guid.Parse("12345678-1234-1234-1234-123456789012") },
                new HeaderValue { Key = "uri", Value = new Uri("rabbitmq:///q/x") },
                new HeaderValue { Key = "timespan", Value = TimeSpan.FromMinutes(90) },
                new HeaderValue { Key = "dateonly", Value = new DateOnly(2026, 8, 3) },
                new HeaderValue { Key = "timeonly", Value = new TimeOnly(10, 30, 15) },
                new HeaderValue { Key = "enum", Value = TestPriority.High },
                new HeaderValue { Key = "element", Value = document.RootElement },
                new HeaderValue { Key = "node", Value = new JsonObject { ["k"] = 1 } },
                new HeaderValue
                {
                    Key = "nested",
                    Value = new Headers([new HeaderValue { Key = "inner", Value = TimeSpan.Zero }])
                },
                new HeaderValue { Key = "set", Value = new HashSet<long> { 7 } }
            ])
        };

        // act
        var headers = RabbitMQMessageEnvelopeFormatter.Format(envelope, s_timeProvider).Headers!;

        // assert
        Assert.Equal(
            new Dictionary<string, object?>
            {
                ["char"] = "c",
                ["guid"] = "12345678-1234-1234-1234-123456789012",
                ["uri"] = "rabbitmq:///q/x",
                ["timespan"] = "01:30:00",
                ["dateonly"] = "2026-08-03",
                ["timeonly"] = "10:30:15.0000000",
                ["enum"] = "High",
                ["element"] = new Dictionary<string, object?> { ["k"] = 1 },
                ["node"] = new Dictionary<string, object?> { ["k"] = 1 },
                ["nested"] = new Dictionary<string, object?> { ["inner"] = "00:00:00" },
                ["set"] = new List<object?> { 7L }
            },
            headers);
    }

    [Fact]
    public void Format_Should_ReadUnzonedTimeAsUtc_When_HeaderIsDateTime()
    {
        // arrange
        var envelope = new MessageEnvelope
        {
            Headers = new Headers(
            [
                new HeaderValue { Key = "unspecified", Value = new DateTime(2024, 1, 15, 10, 30, 45) },
                new HeaderValue { Key = "utc", Value = new DateTime(2024, 1, 15, 10, 30, 45, DateTimeKind.Utc) }
            ])
        };

        // act
        var headers = RabbitMQMessageEnvelopeFormatter.Format(envelope, s_timeProvider).Headers!;

        // assert
        Assert.Equal(
            new AmqpTimestamp(1705314645),
            Assert.IsType<AmqpTimestamp>(headers["unspecified"]));
        Assert.Equal(headers["utc"], headers["unspecified"]);
    }

    [Fact]
    public void Format_Should_ProduceTheEpochFloor_When_HeaderIsTheDefaultDateTime()
    {
        // arrange
        var envelope = new MessageEnvelope
        {
            Headers = new Headers([new HeaderValue { Key = "at", Value = default(DateTime) }])
        };

        // act
        var headers = RabbitMQMessageEnvelopeFormatter.Format(envelope, s_timeProvider).Headers!;

        // assert
        Assert.Equal(
            DateTimeOffset.MinValue.ToUnixTimeSeconds(),
            Assert.IsType<AmqpTimestamp>(headers["at"]).UnixTime);
    }

    [Fact]
    public void Format_Should_CarryDecimalAsText_When_ItExceedsTheTableMantissa()
    {
        // arrange
        // the table's decimal holds a 32 bit mantissa, so a longer one travels as text
        var envelope = new MessageEnvelope
        {
            Headers = new Headers(
            [
                new HeaderValue { Key = "fits", Value = 9.95m },
                new HeaderValue { Key = "exceeds", Value = 21474836.48m },
                new HeaderValue { Key = "negative", Value = -21474836.48m }
            ])
        };

        // act
        var headers = RabbitMQMessageEnvelopeFormatter.Format(envelope, s_timeProvider).Headers!;

        // assert
        Assert.Equal(
            new Dictionary<string, object?>
            {
                ["fits"] = 9.95m,
                ["exceeds"] = "21474836.48",
                ["negative"] = "-21474836.48"
            },
            headers);
    }

    [Fact]
    public void Format_Should_MapJsonToTableShapes_When_HeaderIsAJsonValue()
    {
        // arrange
        // writing the JSON text instead would hand the consumer a string holding JSON, quotes and all
        var envelope = new MessageEnvelope
        {
            Headers = new Headers(
            [
                new HeaderValue { Key = "object", Value = new JsonObject { ["k"] = 1 } },
                new HeaderValue { Key = "array", Value = new JsonArray(1, 2) },
                new HeaderValue { Key = "text", Value = JsonValue.Create("abc") },
                new HeaderValue { Key = "number", Value = JsonValue.Create(5) }
            ])
        };

        // act
        var headers = RabbitMQMessageEnvelopeFormatter.Format(envelope, s_timeProvider).Headers!;

        // assert
        Assert.Equal(
            new Dictionary<string, object?>
            {
                ["object"] = new Dictionary<string, object?> { ["k"] = 1 },
                ["array"] = new List<object?> { 1, 2 },
                ["text"] = "abc",
                ["number"] = 5
            },
            headers);
    }

    [Fact]
    public void Format_Should_KeepEscapes_When_HeaderIsAUri()
    {
        // arrange
        // ToString would decode %20 to a space, leaving text that no longer reads back as this URI
        var envelope = new MessageEnvelope
        {
            Headers = new Headers(
            [
                new HeaderValue { Key = "callback", Value = new Uri("https://example.com/a%20b") }
            ])
        };

        // act
        var headers = RabbitMQMessageEnvelopeFormatter.Format(envelope, s_timeProvider).Headers!;

        // assert
        Assert.Equal("https://example.com/a%20b", headers["callback"]);
    }

    [Fact]
    public void Format_Should_MapSiblingValues_When_ATableKeyIsNotText()
    {
        // arrange
        var envelope = new MessageEnvelope
        {
            Headers = new Headers(
            [
                new HeaderValue
                {
                    Key = "meta",
                    Value = new Hashtable
                    {
                        [1] = "a",
                        ["time"] = DateTimeOffset.FromUnixTimeSeconds(1700000000)
                    }
                }
            ])
        };

        // act
        var headers = RabbitMQMessageEnvelopeFormatter.Format(envelope, s_timeProvider).Headers!;

        // assert
        Assert.Equal(
            new Dictionary<string, object?> { ["1"] = "a", ["time"] = new AmqpTimestamp(1700000000) },
            Assert.IsType<Dictionary<string, object?>>(headers["meta"]));
    }

    [Fact]
    public void Format_Should_DeclareBinary_When_HeaderIsAByteMemory()
    {
        // arrange
        var envelope = new MessageEnvelope
        {
            Headers = new Headers(
            [
                new HeaderValue { Key = "readonly", Value = new ReadOnlyMemory<byte>("hi"u8.ToArray()) },
                new HeaderValue { Key = "writable", Value = new Memory<byte>("hi"u8.ToArray()) },
                new HeaderValue { Key = "empty", Value = default(ArraySegment<byte>) }
            ])
        };

        // act
        var headers = RabbitMQMessageEnvelopeFormatter.Format(envelope, s_timeProvider).Headers!;

        // assert
        Assert.Equal("hi"u8.ToArray(), Assert.IsType<BinaryTableValue>(headers["readonly"]).Bytes);
        Assert.Equal("hi"u8.ToArray(), Assert.IsType<BinaryTableValue>(headers["writable"]).Bytes);
        Assert.Empty(Assert.IsType<BinaryTableValue>(headers["empty"]).Bytes);
    }

    [Fact]
    public void Format_Should_ThrowNamingTheHeader_When_AValueContainsItself()
    {
        // arrange
        var cyclic = new List<object?>();
        cyclic.Add(cyclic);
        var envelope = new MessageEnvelope
        {
            Headers = new Headers([new HeaderValue { Key = "x-cycle", Value = cyclic }])
        };

        // act
        var exception = Record.Exception(() => RabbitMQMessageEnvelopeFormatter.Format(envelope, s_timeProvider).Headers!);

        // assert
        Assert.Equal(
            "The header 'x-cycle' exceeds the maximum AMQP field-table nesting depth of 64.",
            Assert.IsType<InvalidOperationException>(exception).Message);
    }

    private enum TestPriority
    {
        Low,
        High
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
