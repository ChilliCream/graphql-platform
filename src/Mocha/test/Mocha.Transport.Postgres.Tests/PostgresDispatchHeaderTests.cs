using System.Collections.Immutable;
using Mocha.Middlewares;

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
        Assert.Equal(2, parsed.EnclosedMessageTypes.Value.Length);
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
        Assert.Equal(deliverBy, parsed.DeliverBy.Value);
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
        Assert.True(parsed.Headers.TryGetValue("x-tenant", out var tenantValue));
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

    private static ReadOnlyMemory<byte> WriteHeadersJson(MessageEnvelope envelope)
    {
        var feature = new JsonHeadersFeature();
        return PostgresMessageHeadersWriter.Write(feature, envelope);
    }
}
