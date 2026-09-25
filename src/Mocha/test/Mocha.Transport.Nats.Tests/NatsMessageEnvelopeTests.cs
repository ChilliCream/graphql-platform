using System.Collections.Immutable;
using System.Text;
using CookieCrumble;
using Mocha.Middlewares;
using Mocha.Transport.Nats.Tests.Helpers;
using Xunit;

namespace Mocha.Transport.Nats.Tests;

public class NatsMessageEnvelopeTests
{
    private static MessageEnvelope CreateEnvelope()
    {
        var headers = new Headers();
        headers.Set("x-tenant", "acme");

        // A repeated header, which is what an inbound message with several values under one key
        // parses back into. Included here so the round trip covers republishing one.
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
        var parsed = NatsMessageEnvelopeParser.Instance.Parse(headers, envelope.Body, deliveryCount: 1);

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

        // The dedup key is written by the dispatch endpoint, which qualifies it by destination
        // subject. Writing the bare identifier here would make any republish inside one stream
        // look like a duplicate.
        Assert.False(headers.ContainsKey(NatsMessageHeaders.DeduplicationKey));
    }

    [Fact]
    public void Parse_Should_TakeTheDeliveryCountFromMetadata_When_ParsingAMessage()
    {
        // arrange
        var envelope = CreateEnvelope();

        // act
        var headers = NatsMessageHeadersWriter.Instance.Write(envelope);
        var parsed = NatsMessageEnvelopeParser.Instance.Parse(headers, envelope.Body, deliveryCount: 3);

        // assert
        Assert.Equal(3, parsed.DeliveryCount);
    }

    [Fact]
    public void Parse_Should_ExcludeTransportKeys_When_RebuildingUserHeaders()
    {
        // arrange
        var envelope = CreateEnvelope();

        // act
        var headers = NatsMessageHeadersWriter.Instance.Write(envelope);
        var parsed = NatsMessageEnvelopeParser.Instance.Parse(headers, envelope.Body, deliveryCount: 0);

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
        var parsed = NatsMessageEnvelopeParser.Instance.Parse(headers, envelope.Body, deliveryCount: 0);

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
        // Mocha's fault middleware puts a stack trace in a header, and NATS rejects a value
        // containing a line break.
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
        // NATS.Net does not validate keys, so an unchecked key here desynchronises the header block
        // for everything else on the connection.
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
        // NATS.Net 3.0 no longer makes headers read-only after publishing, so sharing one instance
        // across concurrent publishes would be unsafe.
        var first = NatsMessageHeadersWriter.Instance.Write(envelope);
        var second = NatsMessageHeadersWriter.Instance.Write(envelope);

        // assert
        Assert.NotSame(first, second);
    }
}
