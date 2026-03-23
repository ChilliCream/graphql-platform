using System.Collections.Immutable;
using System.Text.Json;
using Mocha.Middlewares;
using Mocha.Utils;

namespace Mocha.Transport.Postgres.Tests;

public class PostgresDispatchHeaderTests
{
    private readonly PostgresMessageEnvelopeParser _parser = PostgresMessageEnvelopeParser.Instance;

    [Fact]
    public void Headers_Should_RoundTrip_ConversationId_When_Set()
    {
        // arrange
        var envelope = new MessageEnvelope { ConversationId = "conv-123" };

        // act
        var parsed = RoundTrip(envelope);

        // assert
        Assert.Equal("conv-123", parsed.ConversationId);
    }

    [Fact]
    public void Headers_Should_RoundTrip_CausationId_When_Set()
    {
        // arrange
        var envelope = new MessageEnvelope { CausationId = "cause-456" };

        // act
        var parsed = RoundTrip(envelope);

        // assert
        Assert.Equal("cause-456", parsed.CausationId);
    }

    [Fact]
    public void Headers_Should_RoundTrip_SourceAddress_When_Set()
    {
        // arrange
        var envelope = new MessageEnvelope { SourceAddress = "postgres:///q/source" };

        // act
        var parsed = RoundTrip(envelope);

        // assert
        Assert.Equal("postgres:///q/source", parsed.SourceAddress);
    }

    [Fact]
    public void Headers_Should_RoundTrip_DestinationAddress_When_Set()
    {
        // arrange
        var envelope = new MessageEnvelope { DestinationAddress = "postgres:///q/destination" };

        // act
        var parsed = RoundTrip(envelope);

        // assert
        Assert.Equal("postgres:///q/destination", parsed.DestinationAddress);
    }

    [Fact]
    public void Headers_Should_RoundTrip_FaultAddress_When_Set()
    {
        // arrange
        var envelope = new MessageEnvelope { FaultAddress = "postgres:///q/fault" };

        // act
        var parsed = RoundTrip(envelope);

        // assert
        Assert.Equal("postgres:///q/fault", parsed.FaultAddress);
    }

    [Fact]
    public void Headers_Should_RoundTrip_EnclosedMessageTypes_When_Set()
    {
        // arrange
        var types = ImmutableArray.Create("urn:message:OrderCreated", "urn:message:IEvent");
        var envelope = new MessageEnvelope { EnclosedMessageTypes = types };

        // act
        var parsed = RoundTrip(envelope);

        // assert
        Assert.NotNull(parsed.EnclosedMessageTypes);
        Assert.Equal(2, parsed.EnclosedMessageTypes!.Value.Length);
        Assert.Equal("urn:message:OrderCreated", parsed.EnclosedMessageTypes.Value[0]);
        Assert.Equal("urn:message:IEvent", parsed.EnclosedMessageTypes.Value[1]);
    }

    [Fact]
    public void Headers_Should_RoundTrip_MessageType_When_Set()
    {
        // arrange
        var envelope = new MessageEnvelope { MessageType = "urn:message:OrderCreated" };

        // act
        var parsed = RoundTrip(envelope);

        // assert
        Assert.Equal("urn:message:OrderCreated", parsed.MessageType);
    }

    [Fact]
    public void Headers_Should_RoundTrip_DeliverBy_When_Set()
    {
        // arrange
        var deliverBy = new DateTimeOffset(2026, 3, 16, 14, 30, 0, TimeSpan.Zero);
        var envelope = new MessageEnvelope { DeliverBy = deliverBy };

        // act
        var parsed = RoundTrip(envelope);

        // assert
        Assert.NotNull(parsed.DeliverBy);
        Assert.Equal(deliverBy, parsed.DeliverBy!.Value);
    }

    [Fact]
    public void Headers_Should_RoundTrip_CustomHeaders_When_Set()
    {
        // arrange
        var envelope = new MessageEnvelope
        {
            Headers = new Headers(
            [
                new HeaderValue { Key = "x-tenant", Value = "acme" },
                new HeaderValue { Key = "x-priority", Value = 5.0 }
            ])
        };

        // act
        var parsed = RoundTrip(envelope);

        // assert
        Assert.NotNull(parsed.Headers);
        Assert.True(parsed.Headers!.TryGetValue("x-tenant", out var tenantValue));
        Assert.Equal("acme", tenantValue);
        Assert.True(parsed.Headers.TryGetValue("x-priority", out var priorityValue));
        Assert.Equal(5.0, priorityValue);
    }

    [Fact]
    public void Headers_Should_ProduceEmptyObject_When_EnvelopeIsEmpty()
    {
        // arrange
        var envelope = new MessageEnvelope();

        // act
        var headerBytes = WriteHeadersJson(envelope);

        // assert - empty envelope should produce empty memory (the serializer returns empty for "{}")
        Assert.True(headerBytes.IsEmpty, "Expected empty headers for an empty envelope");
    }

    /// <summary>
    /// Serializes the envelope headers to JSON using the same logic as
    /// <see cref="PostgresDispatchEndpoint"/>, then parses them back through
    /// <see cref="PostgresMessageEnvelopeParser"/> and returns the round-tripped envelope.
    /// </summary>
    private MessageEnvelope RoundTrip(MessageEnvelope envelope)
    {
        var headerBytes = WriteHeadersJson(envelope);

        var messageItem = new PostgresMessageItem(
            TransportMessageId: Guid.NewGuid(),
            Body: envelope.Body,
            Headers: headerBytes,
            QueueId: 1,
            SentTime: DateTime.UtcNow,
            DeliveryCount: 0,
            MaxDeliveryCount: 10,
            ErrorReason: null);

        return _parser.Parse(messageItem);
    }

    /// <summary>
    /// Replicates the JSON header serialization logic from
    /// <c>PostgresDispatchEndpoint.WriteHeadersJson</c>. This mirrors the production code
    /// exactly so the tests verify that serialization and parsing are symmetric.
    /// </summary>
    private static ReadOnlyMemory<byte> WriteHeadersJson(MessageEnvelope envelope)
    {
        using var writer = new PooledArrayWriter();
        using var jsonWriter = new Utf8JsonWriter(writer);

        jsonWriter.WriteStartObject();

        if (envelope.MessageId is not null)
        {
            jsonWriter.WriteString(PostgresMessageHeaders.MessageId, envelope.MessageId);
        }

        if (envelope.CorrelationId is not null)
        {
            jsonWriter.WriteString(PostgresMessageHeaders.CorrelationId, envelope.CorrelationId);
        }

        if (envelope.ConversationId is not null)
        {
            jsonWriter.WriteString(PostgresMessageHeaders.ConversationId, envelope.ConversationId);
        }

        if (envelope.CausationId is not null)
        {
            jsonWriter.WriteString(PostgresMessageHeaders.CausationId, envelope.CausationId);
        }

        if (envelope.SourceAddress is not null)
        {
            jsonWriter.WriteString(PostgresMessageHeaders.SourceAddress, envelope.SourceAddress);
        }

        if (envelope.DestinationAddress is not null)
        {
            jsonWriter.WriteString(PostgresMessageHeaders.DestinationAddress, envelope.DestinationAddress);
        }

        if (envelope.ResponseAddress is not null)
        {
            jsonWriter.WriteString(PostgresMessageHeaders.ResponseAddress, envelope.ResponseAddress);
        }

        if (envelope.FaultAddress is not null)
        {
            jsonWriter.WriteString(PostgresMessageHeaders.FaultAddress, envelope.FaultAddress);
        }

        if (envelope.ContentType is not null)
        {
            jsonWriter.WriteString(PostgresMessageHeaders.ContentType, envelope.ContentType);
        }

        if (envelope.MessageType is not null)
        {
            jsonWriter.WriteString(PostgresMessageHeaders.MessageType, envelope.MessageType);
        }

        if (envelope.EnclosedMessageTypes is { Length: > 0 } enclosedTypes)
        {
            jsonWriter.WriteStartArray(PostgresMessageHeaders.EnclosedMessageTypes);

            foreach (var type in enclosedTypes)
            {
                jsonWriter.WriteStringValue(type);
            }

            jsonWriter.WriteEndArray();
        }

        if (envelope.DeliverBy is { } deliverBy)
        {
            jsonWriter.WriteString(PostgresMessageHeaders.DeliverBy, deliverBy.ToString("O"));
        }

        if (envelope.Headers is not null)
        {
            foreach (var header in envelope.Headers)
            {
                if (header.Value is not null)
                {
                    jsonWriter.WritePropertyName(header.Key);
                    JsonSerializer.Serialize(jsonWriter, header.Value, header.Value.GetType());
                }
            }
        }

        jsonWriter.WriteEndObject();
        jsonWriter.Flush();

        var bytes = writer.WrittenMemory;
        return bytes.Length <= 2 ? ReadOnlyMemory<byte>.Empty : bytes.ToArray();
    }
}
