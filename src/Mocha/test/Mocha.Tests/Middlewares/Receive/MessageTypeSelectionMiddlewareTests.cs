using Mocha.Middlewares;

namespace Mocha.Tests.Middlewares.Receive;

/// <summary>
/// Tests for <see cref="MessageTypeSelectionMiddleware"/>.
/// Verifies that the middleware correctly resolves the MessageType on the receive context
/// from the envelope's identity string or enclosed message types.
/// </summary>
public class MessageTypeSelectionMiddlewareTests : ReceiveMiddlewareTestBase
{
    [Fact]
    public async Task InvokeAsync_Should_ResolveMessageType_When_EnvelopeHasIdentity()
    {
        // arrange
        var expectedType = new MessageType();
        var registry = new MockMessageTypeRegistry();
        registry.Register("TestEvent", expectedType);

        var middleware = new MessageTypeSelectionMiddleware(registry);
        var context = new StubReceiveContext { Envelope = CreateEnvelope(messageType: "TestEvent") };
        var next = CreatePassthroughDelegate();

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        Assert.Same(expectedType, context.MessageType);
    }

    [Fact]
    public async Task InvokeAsync_Should_NotOverride_When_MessageTypeAlreadySet()
    {
        // arrange
        var existingType = new MessageType();
        var otherType = new MessageType();
        var registry = new MockMessageTypeRegistry();
        registry.Register("TestEvent", otherType);

        var middleware = new MessageTypeSelectionMiddleware(registry);
        var context = new StubReceiveContext
        {
            MessageType = existingType,
            Envelope = CreateEnvelope(messageType: "TestEvent")
        };
        var next = CreatePassthroughDelegate();

        // act
        await middleware.InvokeAsync(context, next);

        // assert - original type is preserved
        Assert.Same(existingType, context.MessageType);
    }

    [Fact]
    public async Task InvokeAsync_Should_FallbackToEnclosedTypes_When_IdentityNotResolved()
    {
        // arrange
        var expectedType = new MessageType();
        var registry = new MockMessageTypeRegistry();
        // "Unknown" is NOT registered, so identity lookup returns null
        registry.Register("EnclosedType", expectedType);

        var middleware = new MessageTypeSelectionMiddleware(registry);
        var context = new StubReceiveContext
        {
            Envelope = CreateEnvelope(messageType: "Unknown", enclosedMessageTypes: ["EnclosedType"])
        };
        var next = CreatePassthroughDelegate();

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        Assert.Same(expectedType, context.MessageType);
    }

    [Fact]
    public async Task InvokeAsync_Should_TakeFirstMatch_When_MultipleEnclosedTypes()
    {
        // arrange
        var firstType = new MessageType();
        var secondType = new MessageType();
        var registry = new MockMessageTypeRegistry();
        // First enclosed type is NOT registered
        registry.Register("SecondType", firstType);
        registry.Register("ThirdType", secondType);

        var middleware = new MessageTypeSelectionMiddleware(registry);
        var context = new StubReceiveContext
        {
            Envelope = CreateEnvelope(
                messageType: "Unknown",
                enclosedMessageTypes: ["UnknownFirst", "SecondType", "ThirdType"])
        };
        var next = CreatePassthroughDelegate();

        // act
        await middleware.InvokeAsync(context, next);

        // assert - stops at first successful lookup
        Assert.Same(firstType, context.MessageType);
    }

    [Fact]
    public async Task InvokeAsync_Should_CallNext_When_MessageTypeNotResolvable()
    {
        // arrange
        var registry = new MockMessageTypeRegistry();
        var middleware = new MessageTypeSelectionMiddleware(registry);
        var context = new StubReceiveContext { Envelope = CreateEnvelope(messageType: "Unknown") };
        var tracker = new InvocationTracker();
        var next = CreateTrackingDelegate(tracker);

        // act
        await middleware.InvokeAsync(context, next);

        // assert - next is called even when type can't be resolved
        Assert.True(tracker.WasInvoked);
        Assert.Null(context.MessageType);
    }

    [Fact]
    public async Task InvokeAsync_Should_CallNext_When_EnvelopeIsNull()
    {
        // arrange
        var registry = new MockMessageTypeRegistry();
        var middleware = new MessageTypeSelectionMiddleware(registry);
        var context = new StubReceiveContext { Envelope = null };
        var tracker = new InvocationTracker();
        var next = CreateTrackingDelegate(tracker);

        // act
        await middleware.InvokeAsync(context, next);

        // assert - handles null envelope gracefully
        Assert.True(tracker.WasInvoked);
        Assert.Null(context.MessageType);
    }

    private sealed class MockMessageTypeRegistry : IMessageTypeRegistry
    {
        private readonly Dictionary<string, MessageType> _typesByIdentity = new();

        public IMessageSerializerRegistry Serializers => null!;
        public IReadOnlySet<MessageType> MessageTypes => new HashSet<MessageType>(_typesByIdentity.Values);

        public void Register(string identity, MessageType messageType) => _typesByIdentity[identity] = messageType;

        public MessageType? GetMessageType(string identity) => _typesByIdentity.GetValueOrDefault(identity);

        public bool IsRegistered(Type type) => false;

        public MessageType? GetMessageType(Type type) => null;

        public void AddMessageType(MessageType messageType) { }

        public MessageType GetOrAdd(IMessagingConfigurationContext context, Type type) => null!;
    }
}
