using System.Collections.Immutable;
using System.Text;
using CookieCrumble;
using Mocha.Middlewares;
using Mocha.Transport.Nats.Tests.Helpers;

namespace Mocha.Transport.Nats.Tests;

public class NatsMessageEnvelopeTests
{
    private static MessageEnvelope CreateEnvelope()
    {
        var headers = new Headers();
        headers.Set("x-tenant", "acme");

        // A repeated header, as an inbound message with several values under one key parses into.
        headers.Set("x-forwarded-for", new[] { "10.0.0.1", "10.0.0.2" });

        return new MessageEnvelope
        {
            MessageId = "01JQ8Z0000000000000000",
            CorrelationId = "correlation-1",
            ConversationId = "conversation-1",
            CausationId = "causation-1",
            SourceAddress = "nats://localhost:4222/ORDER_SERVICE/s/order-service.order-created",
            DestinationAddress = "nats://localhost:4222/ORDER_SERVICE/c/order-service_order-created",
            ResponseAddress = "_INBOX.abc123",
            FaultAddress = "nats://localhost:4222/ORDER_SERVICE/s/order-created_error",
            MessageType = "Contracts.OrderCreated",
            ContentType = "application/json",
            SentAt = new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero),
            DeliverBy = new DateTimeOffset(2026, 8, 10, 12, 5, 0, TimeSpan.Zero),
            ScheduledTime = new DateTimeOffset(2026, 8, 10, 12, 1, 0, TimeSpan.Zero),
            EnclosedMessageTypes = ImmutableArray.Create("Contracts.OrderCreated", "Contracts.IOrderEvent"),
            Headers = headers,
            Body = Encoding.UTF8.GetBytes("""{"orderId":"1"}""")
        };
    }

    [Fact]
    public void Write_Should_RoundTripEveryField_When_ParsedBack()
    {
        // arrange
        var envelope = CreateEnvelope();

        // act
        var headers = NatsMessageHeadersWriter.Instance.Write(envelope);
        var parsed = NatsMessageEnvelopeParser.Instance.Parse(headers, envelope.Body, deliveryCount: 1, storedAt: null);

        // assert
        new Snapshot()
            .Add(NatsEnvelopeSnapshot.Create(headers), "Headers", MarkdownLanguages.Json)
            .Add(NatsEnvelopeSnapshot.Create(parsed), "ParsedEnvelope", MarkdownLanguages.Json)
            .MatchMarkdown();
    }

    [Fact]
    public void Write_Should_KeepTheMessageIdSeparate_From_TheDeduplicationKey()
    {
        // arrange
        var envelope = CreateEnvelope();

        // act
        var headers = NatsMessageHeadersWriter.Instance.Write(envelope);

        // assert
        Assert.True(headers.TryGetLastValue(NatsMessageHeaders.MessageId, out var messageId));
        Assert.Equal(envelope.MessageId, messageId);

        Assert.False(headers.ContainsKey(NatsMessageHeaders.DeduplicationKey));
    }

    [Fact]
    public void Parse_Should_TakeTheDeliveryCountFromMetadata_When_ParsingAMessage()
    {
        // arrange
        var envelope = CreateEnvelope();

        // act
        var headers = NatsMessageHeadersWriter.Instance.Write(envelope);
        var parsed = NatsMessageEnvelopeParser.Instance.Parse(headers, envelope.Body, deliveryCount: 3, storedAt: null);

        // assert
        Assert.Equal(3, parsed.DeliveryCount);
    }

    [Fact]
    public void Parse_Should_UseTheStoredTime_When_TheSenderSetNoSentAt()
    {
        // arrange
        var envelope = new MessageEnvelope
        {
            MessageId = "message-1",
            Body = Encoding.UTF8.GetBytes("{}")
        };
        var storedAt = new DateTimeOffset(2026, 10, 26, 9, 30, 0, TimeSpan.Zero);

        // act
        var headers = NatsMessageHeadersWriter.Instance.Write(envelope);
        var parsed = NatsMessageEnvelopeParser.Instance.Parse(headers, envelope.Body, 1, storedAt);

        // assert
        Assert.Equal(storedAt, parsed.SentAt);
    }

    [Fact]
    public void Parse_Should_PreferTheSendersSentAt_When_BothAreKnown()
    {
        // arrange
        var envelope = CreateEnvelope();
        var storedAt = envelope.SentAt!.Value.AddSeconds(2);

        // act
        var headers = NatsMessageHeadersWriter.Instance.Write(envelope);
        var parsed = NatsMessageEnvelopeParser.Instance.Parse(headers, envelope.Body, 1, storedAt);

        // assert
        Assert.Equal(envelope.SentAt, parsed.SentAt);
    }

    [Fact]
    public void Parse_Should_ExcludeTransportKeys_When_RebuildingUserHeaders()
    {
        // arrange
        var envelope = CreateEnvelope();

        // act
        var headers = NatsMessageHeadersWriter.Instance.Write(envelope);
        var parsed = NatsMessageEnvelopeParser.Instance.Parse(headers, envelope.Body, deliveryCount: 0, storedAt: null);

        // assert
        var parsedHeaders = Assert.IsType<Headers>(parsed.Headers);

        Assert.Equal("acme", parsedHeaders.GetValue("x-tenant"));
        Assert.False(parsedHeaders.ContainsKey("Nats-Msg-Id"));
        Assert.False(parsedHeaders.ContainsKey("x-message-type"));
    }

    [Fact]
    public void Parse_Should_LeaveOptionalFieldsNull_When_TheHeadersAreAbsent()
    {
        // arrange
        var envelope = new MessageEnvelope { Body = Encoding.UTF8.GetBytes("{}") };

        // act
        var headers = NatsMessageHeadersWriter.Instance.Write(envelope);
        var parsed = NatsMessageEnvelopeParser.Instance.Parse(headers, envelope.Body, deliveryCount: 0, storedAt: null);

        // assert
        Assert.Null(parsed.MessageId);
        Assert.Null(parsed.CorrelationId);
        Assert.Null(parsed.SentAt);
        Assert.Null(parsed.ScheduledTime);
        Assert.Empty(parsed.EnclosedMessageTypes ?? []);
    }

    [Fact]
    public void Write_Should_FlattenLineBreaks_When_AValueSpansLines()
    {
        // arrange
        // NATS rejects a header value containing a line break.
        var headers = new Headers();
        headers.Set("fault-stack-trace", "at Handler.HandleAsync()\r\n   at Pipeline.InvokeAsync()");

        var envelope = new MessageEnvelope
        {
            Headers = headers,
            Body = Encoding.UTF8.GetBytes("{}")
        };

        // act
        var written = NatsMessageHeadersWriter.Instance.Write(envelope);

        // assert
        Assert.True(written.TryGetLastValue("fault-stack-trace", out var value));
        Assert.Equal("at Handler.HandleAsync()     at Pipeline.InvokeAsync()", value);
    }

    [Fact]
    public void Write_Should_PreserveEveryValue_When_AHeaderIsRepeated()
    {
        // arrange
        var headers = new Headers();
        headers.Set("x-forwarded-for", new[] { "10.0.0.1", "10.0.0.2" });

        var envelope = new MessageEnvelope
        {
            Headers = headers,
            Body = Encoding.UTF8.GetBytes("{}")
        };

        // act
        var written = NatsMessageHeadersWriter.Instance.Write(envelope);

        // assert
        var values = written["x-forwarded-for"].Select(static v => v!).ToList();

        Assert.Equal(new List<string> { "10.0.0.1", "10.0.0.2" }, values);
    }

    [Theory]
    [InlineData("bad:key")]
    [InlineData("bad\r\nkey")]
    [InlineData("bad key")]
    public void Write_Should_Throw_When_AHeaderKeyWouldCorruptFraming(string key)
    {
        // arrange
        // NATS.Net does not validate header keys.
        var headers = new Headers();
        headers.Set(key, "value");

        var envelope = new MessageEnvelope
        {
            Headers = headers,
            Body = Encoding.UTF8.GetBytes("{}")
        };

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => NatsMessageHeadersWriter.Instance.Write(envelope));

        // assert
        Assert.Equal(
            $"Header '{key}' cannot be sent over NATS. Header keys cannot contain ':', whitespace "
            + "or control characters.",
            exception.Message);
    }

    [Fact]
    public void Write_Should_ReturnAFreshInstance_When_CalledRepeatedly()
    {
        // arrange
        var envelope = CreateEnvelope();

        // act
        var first = NatsMessageHeadersWriter.Instance.Write(envelope);
        var second = NatsMessageHeadersWriter.Instance.Write(envelope);

        // assert
        Assert.NotSame(first, second);
    }
}
