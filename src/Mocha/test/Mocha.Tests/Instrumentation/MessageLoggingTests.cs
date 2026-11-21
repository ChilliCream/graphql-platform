using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Mocha.Middlewares;

namespace Mocha.Tests.Instrumentation;

public sealed class MessageLoggingTests
{
    [Fact]
    public void BeginMessageScope_Should_KeepCapturedIdentifiers_When_ContextChangesAfterward()
    {
        // arrange
        var logger = new FakeLogger();
        var context = CreateReceiveContext();

        // act
        using var _ = logger.BeginMessageScope(LogLevel.Warning, context);
        // a pooled context is reset and reused for the next message
        context.MessageId = "message-2";
        context.DeliveryCount = 1;
        logger.LogWarning("Probe");

        // assert
        KeyValuePair<string, object?>[] expected =
        [
            new("InstanceId", null),
            new("Transport", null),
            new("Endpoint", null),
            new("SourceAddress", null),
            new("MessageId", "message-1"),
            new("MessageType", null),
            new("CorrelationId", "correlation-1"),
            new("ConversationId", "conversation-1"),
            new("CausationId", "causation-1"),
            new("DeliveryCount", 2)
        ];
        var scope = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object?>>>(
            Assert.Single(logger.LatestRecord.Scopes));
        Assert.Equal(expected, scope);
    }

    [Fact]
    public void BeginMessageScope_Should_ReturnNull_When_LevelIsNotEnabled()
    {
        // arrange
        var logger = new FakeLogger();
        logger.ControlLevel(LogLevel.Warning, false);
        var context = CreateReceiveContext();

        // act
        var scope = logger.BeginMessageScope(LogLevel.Warning, context);

        // assert
        Assert.Null(scope);
        Assert.Empty(logger.Collector.GetSnapshot());
    }

    [Fact]
    public void BeginMessageScope_Should_WriteKeyValueText_When_ScopeIsFormatted()
    {
        // arrange
        var logger = new FakeLogger();
        var context = CreateReceiveContext();

        // act
        using var _ = logger.BeginMessageScope(LogLevel.Warning, context);
        logger.LogWarning("Probe");
        var text = Assert.Single(logger.LatestRecord.Scopes)?.ToString();

        // assert
        Assert.Equal(
            "InstanceId: Transport: Endpoint: SourceAddress: MessageId:message-1 MessageType: "
            + "CorrelationId:correlation-1 ConversationId:conversation-1 CausationId:causation-1 DeliveryCount:2",
            text);
    }

    [Fact]
    public void BeginMessageScope_Should_PreferEnvelopeIdentifiers_When_DispatchContextHasEnvelope()
    {
        // arrange
        var logger = new FakeLogger();
        var context = new DispatchContext
        {
            DestinationAddress = new Uri("queue://orders"),
            MessageId = "context-message",
            CorrelationId = "context-correlation",
            ConversationId = "context-conversation",
            CausationId = "context-causation",
            Envelope = new MessageEnvelope
            {
                MessageId = "envelope-message",
                MessageType = "urn:message:order-placed",
                CorrelationId = "envelope-correlation",
                ConversationId = "envelope-conversation",
                CausationId = "envelope-causation"
            }
        };

        // act
        using var _ = logger.BeginMessageScope(LogLevel.Warning, context);
        logger.LogWarning("Probe");

        // assert
        KeyValuePair<string, object?>[] expected =
        [
            new("InstanceId", null),
            new("Transport", null),
            new("Endpoint", null),
            new("DestinationAddress", new Uri("queue://orders")),
            new("MessageId", "envelope-message"),
            new("MessageType", "urn:message:order-placed"),
            new("CorrelationId", "envelope-correlation"),
            new("ConversationId", "envelope-conversation"),
            new("CausationId", "envelope-causation")
        ];
        var scope = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object?>>>(
            Assert.Single(logger.LatestRecord.Scopes));
        Assert.Equal(expected, scope);
    }

    private static ReceiveContext CreateReceiveContext()
        => new()
        {
            MessageId = "message-1",
            CorrelationId = "correlation-1",
            ConversationId = "conversation-1",
            CausationId = "causation-1",
            DeliveryCount = 2
        };
}
