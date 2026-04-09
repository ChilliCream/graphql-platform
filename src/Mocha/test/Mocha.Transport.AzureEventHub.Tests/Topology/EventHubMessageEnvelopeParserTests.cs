using Azure.Messaging.EventHubs;

namespace Mocha.Transport.AzureEventHub.Tests.Topology;

public class EventHubMessageEnvelopeParserTests
{
    private readonly EventHubMessageEnvelopeParser _parser = EventHubMessageEnvelopeParser.Instance;

    [Fact]
    public void Parse_Should_ExtractBody_When_EventDataHasPayload()
    {
        // arrange
        var body = new byte[] { 1, 2, 3, 4, 5 };
        var eventData = new EventData(body);

        // act
        var envelope = _parser.Parse(eventData);

        // assert
        Assert.Equal(body, envelope.Body.ToArray());
    }

    [Fact]
    public void Parse_Should_ExtractMessageId_When_SetOnAmqpProperties()
    {
        // arrange
        var eventData = new EventData(new byte[] { 0 });
        var amqp = eventData.GetRawAmqpMessage();
        amqp.Properties.MessageId = new Azure.Core.Amqp.AmqpMessageId("msg-123");

        // act
        var envelope = _parser.Parse(eventData);

        // assert
        Assert.Equal("msg-123", envelope.MessageId);
    }

    [Fact]
    public void Parse_Should_ExtractCorrelationId_When_SetOnAmqpProperties()
    {
        // arrange
        var eventData = new EventData(new byte[] { 0 });
        var amqp = eventData.GetRawAmqpMessage();
        amqp.Properties.CorrelationId = new Azure.Core.Amqp.AmqpMessageId("corr-456");

        // act
        var envelope = _parser.Parse(eventData);

        // assert
        Assert.Equal("corr-456", envelope.CorrelationId);
    }

    [Fact]
    public void Parse_Should_ExtractContentType_When_SetOnAmqpProperties()
    {
        // arrange
        var eventData = new EventData(new byte[] { 0 });
        var amqp = eventData.GetRawAmqpMessage();
        amqp.Properties.ContentType = "application/json";

        // act
        var envelope = _parser.Parse(eventData);

        // assert
        Assert.Equal("application/json", envelope.ContentType);
    }

    [Fact]
    public void Parse_Should_ExtractMessageType_When_SubjectSet()
    {
        // arrange
        var eventData = new EventData(new byte[] { 0 });
        var amqp = eventData.GetRawAmqpMessage();
        amqp.Properties.Subject = "OrderCreated";

        // act
        var envelope = _parser.Parse(eventData);

        // assert
        Assert.Equal("OrderCreated", envelope.MessageType);
    }

    [Fact]
    public void Parse_Should_ExtractResponseAddress_When_ReplyToSet()
    {
        // arrange
        var eventData = new EventData(new byte[] { 0 });
        var amqp = eventData.GetRawAmqpMessage();
        amqp.Properties.ReplyTo = new Azure.Core.Amqp.AmqpAddress("eventhub:///replies");

        // act
        var envelope = _parser.Parse(eventData);

        // assert
        Assert.Equal("eventhub:///replies", envelope.ResponseAddress);
    }

    [Fact]
    public void Parse_Should_ExtractConversationId_When_SetInApplicationProperties()
    {
        // arrange
        var eventData = new EventData(new byte[] { 0 });
        var amqp = eventData.GetRawAmqpMessage();
        amqp.ApplicationProperties[EventHubMessageHeaders.ConversationId] = "conv-789";

        // act
        var envelope = _parser.Parse(eventData);

        // assert
        Assert.Equal("conv-789", envelope.ConversationId);
    }

    [Fact]
    public void Parse_Should_ExtractCausationId_When_SetInApplicationProperties()
    {
        // arrange
        var eventData = new EventData(new byte[] { 0 });
        var amqp = eventData.GetRawAmqpMessage();
        amqp.ApplicationProperties[EventHubMessageHeaders.CausationId] = "cause-101";

        // act
        var envelope = _parser.Parse(eventData);

        // assert
        Assert.Equal("cause-101", envelope.CausationId);
    }

    [Fact]
    public void Parse_Should_ExtractSourceAddress_When_SetInApplicationProperties()
    {
        // arrange
        var eventData = new EventData(new byte[] { 0 });
        var amqp = eventData.GetRawAmqpMessage();
        amqp.ApplicationProperties[EventHubMessageHeaders.SourceAddress] = "eventhub:///h/source-hub";

        // act
        var envelope = _parser.Parse(eventData);

        // assert
        Assert.Equal("eventhub:///h/source-hub", envelope.SourceAddress);
    }

    [Fact]
    public void Parse_Should_ExtractDestinationAddress_When_SetInApplicationProperties()
    {
        // arrange
        var eventData = new EventData(new byte[] { 0 });
        var amqp = eventData.GetRawAmqpMessage();
        amqp.ApplicationProperties[EventHubMessageHeaders.DestinationAddress] = "eventhub:///h/dest-hub";

        // act
        var envelope = _parser.Parse(eventData);

        // assert
        Assert.Equal("eventhub:///h/dest-hub", envelope.DestinationAddress);
    }

    [Fact]
    public void Parse_Should_ExtractFaultAddress_When_SetInApplicationProperties()
    {
        // arrange
        var eventData = new EventData(new byte[] { 0 });
        var amqp = eventData.GetRawAmqpMessage();
        amqp.ApplicationProperties[EventHubMessageHeaders.FaultAddress] = "eventhub:///h/error";

        // act
        var envelope = _parser.Parse(eventData);

        // assert
        Assert.Equal("eventhub:///h/error", envelope.FaultAddress);
    }

    [Fact]
    public void Parse_Should_ExtractSentAt_When_StoredAsUnixMilliseconds()
    {
        // arrange
        var sentAt = new DateTimeOffset(2026, 3, 27, 12, 0, 0, TimeSpan.Zero);
        var eventData = new EventData(new byte[] { 0 });
        var amqp = eventData.GetRawAmqpMessage();
        amqp.ApplicationProperties[EventHubMessageHeaders.SentAt] = sentAt.ToUnixTimeMilliseconds();

        // act
        var envelope = _parser.Parse(eventData);

        // assert
        Assert.NotNull(envelope.SentAt);
        Assert.Equal(sentAt, envelope.SentAt.Value);
    }

    [Fact]
    public void Parse_Should_ExtractEnclosedMessageTypes_When_SemicolonDelimited()
    {
        // arrange
        var eventData = new EventData(new byte[] { 0 });
        var amqp = eventData.GetRawAmqpMessage();
        amqp.ApplicationProperties[EventHubMessageHeaders.EnclosedMessageTypes] = "OrderCreated;IEvent";

        // act
        var envelope = _parser.Parse(eventData);

        // assert
        Assert.NotNull(envelope.EnclosedMessageTypes);
        Assert.Equal(2, envelope.EnclosedMessageTypes.Value.Length);
        Assert.Equal("OrderCreated", envelope.EnclosedMessageTypes.Value[0]);
        Assert.Equal("IEvent", envelope.EnclosedMessageTypes.Value[1]);
    }

    [Fact]
    public void Parse_Should_ReturnEmptyTypes_When_EnclosedMessageTypesNotSet()
    {
        // arrange
        var eventData = new EventData(new byte[] { 0 });

        // act
        var envelope = _parser.Parse(eventData);

        // assert
        Assert.NotNull(envelope.EnclosedMessageTypes);
        Assert.True(envelope.EnclosedMessageTypes.Value.IsEmpty);
    }

    [Fact]
    public void Parse_Should_ExtractCustomHeaders_When_NonWellKnownPropertiesPresent()
    {
        // arrange
        var eventData = new EventData(new byte[] { 0 });
        var amqp = eventData.GetRawAmqpMessage();
        amqp.ApplicationProperties["x-tenant"] = "acme";
        amqp.ApplicationProperties["x-trace-id"] = "trace-123";

        // act
        var envelope = _parser.Parse(eventData);

        // assert
        Assert.NotNull(envelope.Headers);
        Assert.True(envelope.Headers.TryGetValue("x-tenant", out var tenant));
        Assert.Equal("acme", tenant);
        Assert.True(envelope.Headers.TryGetValue("x-trace-id", out var traceId));
        Assert.Equal("trace-123", traceId);
    }

    [Fact]
    public void Parse_Should_ExcludeWellKnownHeaders_When_BuildingCustomHeaders()
    {
        // arrange
        var eventData = new EventData(new byte[] { 0 });
        var amqp = eventData.GetRawAmqpMessage();
        amqp.ApplicationProperties[EventHubMessageHeaders.ConversationId] = "conv-1";
        amqp.ApplicationProperties["x-custom"] = "custom-value";

        // act
        var envelope = _parser.Parse(eventData);

        // assert
        Assert.NotNull(envelope.Headers);
        Assert.True(envelope.Headers.TryGetValue("x-custom", out var custom));
        Assert.Equal("custom-value", custom);
        Assert.False(envelope.Headers.TryGetValue(EventHubMessageHeaders.ConversationId, out _));
    }

    [Fact]
    public void Parse_Should_RoundTripAllFields_When_FullyPopulatedEventData()
    {
        // arrange
        var body = new byte[] { 10, 20, 30 };
        var sentAt = new DateTimeOffset(2026, 3, 27, 15, 30, 0, TimeSpan.Zero);
        var eventData = new EventData(body);
        var amqp = eventData.GetRawAmqpMessage();

        amqp.Properties.MessageId = new Azure.Core.Amqp.AmqpMessageId("msg-full");
        amqp.Properties.CorrelationId = new Azure.Core.Amqp.AmqpMessageId("corr-full");
        amqp.Properties.ContentType = "application/json";
        amqp.Properties.Subject = "OrderCreated";
        amqp.Properties.ReplyTo = new Azure.Core.Amqp.AmqpAddress("eventhub:///replies");

        amqp.ApplicationProperties[EventHubMessageHeaders.ConversationId] = "conv-full";
        amqp.ApplicationProperties[EventHubMessageHeaders.CausationId] = "cause-full";
        amqp.ApplicationProperties[EventHubMessageHeaders.SourceAddress] = "eventhub:///h/source";
        amqp.ApplicationProperties[EventHubMessageHeaders.DestinationAddress] = "eventhub:///h/dest";
        amqp.ApplicationProperties[EventHubMessageHeaders.FaultAddress] = "eventhub:///h/error";
        amqp.ApplicationProperties[EventHubMessageHeaders.SentAt] = sentAt.ToUnixTimeMilliseconds();
        amqp.ApplicationProperties[EventHubMessageHeaders.EnclosedMessageTypes] = "OrderCreated;IEvent";
        amqp.ApplicationProperties["x-tenant"] = "acme";

        // act
        var envelope = _parser.Parse(eventData);

        // assert
        Assert.Equal(body, envelope.Body.ToArray());
        Assert.Equal("msg-full", envelope.MessageId);
        Assert.Equal("corr-full", envelope.CorrelationId);
        Assert.Equal("application/json", envelope.ContentType);
        Assert.Equal("OrderCreated", envelope.MessageType);
        Assert.Equal("eventhub:///replies", envelope.ResponseAddress);
        Assert.Equal("conv-full", envelope.ConversationId);
        Assert.Equal("cause-full", envelope.CausationId);
        Assert.Equal("eventhub:///h/source", envelope.SourceAddress);
        Assert.Equal("eventhub:///h/dest", envelope.DestinationAddress);
        Assert.Equal("eventhub:///h/error", envelope.FaultAddress);
        Assert.Equal(sentAt, envelope.SentAt);
        Assert.NotNull(envelope.EnclosedMessageTypes);
        Assert.Equal(2, envelope.EnclosedMessageTypes.Value.Length);
        Assert.NotNull(envelope.Headers);
        Assert.True(envelope.Headers.TryGetValue("x-tenant", out var tenant));
        Assert.Equal("acme", tenant);
    }
}
