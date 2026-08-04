using System.Collections.Immutable;
using Mocha.Middlewares;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests;

public class RabbitMQDispatchContextExtensionsTests
{
    [Fact]
    public void BuildHeaders_Should_ConvertDateTimeOffset_When_HeaderContainsDateTimeOffset()
    {
        // arrange
        var dto = new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero);
        var envelope = new MessageEnvelope
        {
            Headers = new Headers([new HeaderValue { Key = "x-timestamp", Value = dto }])
        };

        // act
        var headers = envelope.BuildHeaders();

        // assert
        var timestamp = Assert.IsType<AmqpTimestamp>(headers["x-timestamp"]);
        Assert.Equal(dto.ToUnixTimeSeconds(), timestamp.UnixTime);
    }

    [Fact]
    public void BuildHeaders_Should_ConvertDateTime_When_HeaderContainsDateTime()
    {
        // arrange
        var dt = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var envelope = new MessageEnvelope
        {
            Headers = new Headers([new HeaderValue { Key = "x-timestamp", Value = dt }])
        };

        // act
        var headers = envelope.BuildHeaders();

        // assert
        var timestamp = Assert.IsType<AmqpTimestamp>(headers["x-timestamp"]);
        var expected = new DateTimeOffset(dt).ToUnixTimeSeconds();
        Assert.Equal(expected, timestamp.UnixTime);
    }

    [Fact]
    public void BuildHeaders_Should_SkipNullValues_When_HeaderValueIsNull()
    {
        // arrange
        var envelope = new MessageEnvelope
        {
            Headers = new Headers([new HeaderValue { Key = "x-null", Value = null }])
        };

        // act
        var headers = envelope.BuildHeaders();

        // assert
        Assert.False(headers.ContainsKey("x-null"));
    }

    [Fact]
    public void BuildHeaders_Should_PassThroughOtherTypes_When_HeaderContainsString()
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
        var headers = envelope.BuildHeaders();

        // assert
        Assert.Equal("hello", headers["x-string"]);
        Assert.Equal(42, headers["x-int"]);
    }

    [Fact]
    public void BuildHeaders_Should_SetConversationId_When_EnvelopeHasConversationId()
    {
        // arrange
        var envelope = new MessageEnvelope { ConversationId = "conv-123" };

        // act
        var headers = envelope.BuildHeaders();

        // assert
        Assert.Equal("conv-123", headers[RabbitMQMessageHeaders.ConversationId.Key]);
    }

    [Fact]
    public void BuildHeaders_Should_SetCausationId_When_EnvelopeHasCausationId()
    {
        // arrange
        var envelope = new MessageEnvelope { CausationId = "cause-456" };

        // act
        var headers = envelope.BuildHeaders();

        // assert
        Assert.Equal("cause-456", headers[RabbitMQMessageHeaders.CausationId.Key]);
    }

    [Fact]
    public void BuildHeaders_Should_SetSourceAddress_When_EnvelopeHasSourceAddress()
    {
        // arrange
        var envelope = new MessageEnvelope { SourceAddress = "rabbitmq:///q/source-q" };

        // act
        var headers = envelope.BuildHeaders();

        // assert
        Assert.Equal("rabbitmq:///q/source-q", headers[RabbitMQMessageHeaders.SourceAddress.Key]);
    }

    [Fact]
    public void BuildHeaders_Should_SetDestinationAddress_When_EnvelopeHasDestinationAddress()
    {
        // arrange
        var envelope = new MessageEnvelope { DestinationAddress = "rabbitmq:///q/dest-q" };

        // act
        var headers = envelope.BuildHeaders();

        // assert
        Assert.Equal("rabbitmq:///q/dest-q", headers[RabbitMQMessageHeaders.DestinationAddress.Key]);
    }

    [Fact]
    public void BuildHeaders_Should_SetFaultAddress_When_EnvelopeHasFaultAddress()
    {
        // arrange
        var envelope = new MessageEnvelope { FaultAddress = "rabbitmq:///q/fault-q" };

        // act
        var headers = envelope.BuildHeaders();

        // assert
        Assert.Equal("rabbitmq:///q/fault-q", headers[RabbitMQMessageHeaders.FaultAddress.Key]);
    }

    [Fact]
    public void BuildHeaders_Should_SetEnclosedMessageTypes_When_EnvelopeHasTypes()
    {
        // arrange
        var types = ImmutableArray.Create("urn:message:OrderCreated", "urn:message:IEvent");
        var envelope = new MessageEnvelope { EnclosedMessageTypes = types };

        // act
        var headers = envelope.BuildHeaders();

        // assert
        Assert.True(headers.ContainsKey(RabbitMQMessageHeaders.EnclosedMessageTypes.Key));
        Assert.Equal(types, headers[RabbitMQMessageHeaders.EnclosedMessageTypes.Key]);
    }

    [Fact]
    public void BuildHeaders_Should_SetMessageType_When_EnvelopeHasMessageType()
    {
        // arrange
        var envelope = new MessageEnvelope { MessageType = "urn:message:OrderCreated" };

        // act
        var headers = envelope.BuildHeaders();

        // assert
        Assert.Equal("urn:message:OrderCreated", headers[RabbitMQMessageHeaders.MessageType.Key]);
    }

    [Fact]
    public void BuildHeaders_Should_ReturnEmptyHeaders_When_EnvelopeIsEmpty()
    {
        // arrange
        var envelope = new MessageEnvelope();

        // act
        var headers = envelope.BuildHeaders();

        // assert
        Assert.Empty(headers);
    }
}
