using Mocha.Features;
using Mocha.Middlewares;

namespace Mocha.Tests.Consumers.Batching;

public sealed class ConsumeContextTests
{
    [Fact]
    public void Message_Should_ResolveEagerly_When_Constructed()
    {
        // arrange
        var inner = CreateStubWithMessage(new TestEvent { Id = "eager-1" });

        // act
        using var ctx = new ConsumeContext<TestEvent>(inner);

        // assert
        Assert.Equal("eager-1", ctx.Message.Id);
    }

    [Fact]
    public void Properties_Should_ForwardToInner_When_Accessed()
    {
        // arrange
        var inner = CreateStubWithMessage(new TestEvent { Id = "fwd" });
        inner.MessageId = "msg-123";
        inner.CorrelationId = "corr-456";
        inner.ConversationId = "conv-789";
        inner.CausationId = "cause-1";
        inner.SentAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        inner.DeliveryCount = 3;

        using var ctx = new ConsumeContext<TestEvent>(inner);

        // act & assert
        Assert.Equal("msg-123", ctx.MessageId);
        Assert.Equal("corr-456", ctx.CorrelationId);
        Assert.Equal("conv-789", ctx.ConversationId);
        Assert.Equal("cause-1", ctx.CausationId);
        Assert.Equal(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), ctx.SentAt);
        Assert.Equal(3, ctx.DeliveryCount);
    }

    [Fact]
    public void Headers_Should_ForwardToInner_When_Accessed()
    {
        // arrange
        var inner = CreateStubWithMessage(new TestEvent { Id = "h" });
        using var ctx = new ConsumeContext<TestEvent>(inner);

        // act & assert
        Assert.Same(inner.Headers, ctx.Headers);
    }

    [Fact]
    public void Features_Should_ForwardToInner_When_Accessed()
    {
        // arrange
        var inner = CreateStubWithMessage(new TestEvent { Id = "f" });
        using var ctx = new ConsumeContext<TestEvent>(inner);

        // act & assert
        Assert.Same(inner.Features, ctx.Features);
    }

    [Fact]
    public void Dispose_Should_CausePropertyAccessToThrow_When_Called()
    {
        // arrange
        var inner = CreateStubWithMessage(new TestEvent { Id = "d" });
        var ctx = new ConsumeContext<TestEvent>(inner);

        // act
        ctx.Dispose();

        // assert - forwarded properties throw after dispose
        Assert.Throws<ObjectDisposedException>(() => ctx.MessageId);
        Assert.Throws<ObjectDisposedException>(() => ctx.CorrelationId);
        Assert.Throws<ObjectDisposedException>(() => ctx.ConversationId);
        Assert.Throws<ObjectDisposedException>(() => ctx.CausationId);
        Assert.Throws<ObjectDisposedException>(() => ctx.SentAt);
        Assert.Throws<ObjectDisposedException>(() => ctx.DeliveryCount);
        Assert.Throws<ObjectDisposedException>(() => ctx.Headers);
        Assert.Throws<ObjectDisposedException>(() => ctx.Features);
        Assert.Throws<ObjectDisposedException>(() => ctx.Body);
        Assert.Throws<ObjectDisposedException>(() => ctx.Host);
        Assert.Throws<ObjectDisposedException>(() => ctx.Runtime);
        Assert.Throws<ObjectDisposedException>(() => ctx.Services);
        Assert.Throws<ObjectDisposedException>(() => ctx.CancellationToken);
    }

    [Fact]
    public void Dispose_Should_CauseSettersToThrow_When_Called()
    {
        // arrange
        var inner = CreateStubWithMessage(new TestEvent { Id = "s" });
        var ctx = new ConsumeContext<TestEvent>(inner);

        // act
        ctx.Dispose();

        // assert
        Assert.Throws<ObjectDisposedException>(() => ctx.MessageId = "x");
        Assert.Throws<ObjectDisposedException>(() => ctx.SentAt = DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Message_Should_StillBeAccessible_When_ResolvedBeforeDispose()
    {
        // arrange
        var inner = CreateStubWithMessage(new TestEvent { Id = "survives" });
        var ctx = new ConsumeContext<TestEvent>(inner);

        // act - resolve before dispose (matches BatchConsumer's pattern)
        _ = ctx.Message;
        ctx.Dispose();

        // assert - cached field survives dispose
        Assert.Equal("survives", ctx.Message.Id);
    }

    // --- Helpers ---

    private static StubConsumeContext CreateStubWithMessage(TestEvent message)
    {
        var stub = new StubConsumeContext();
        stub.SetMessage(message);
        return stub;
    }

    // --- Test types ---

    public sealed class TestEvent
    {
        public required string Id { get; init; }
    }

    private sealed class StubConsumeContext : IConsumeContext
    {
        private readonly FeatureCollection _features = new();

        public StubConsumeContext()
        {
            Features = _features;
        }

        public void SetMessage(object message)
        {
            _features.GetOrSet<MessageParsingFeature>().Message = message;
        }

        public IFeatureCollection Features { get; }
        public IReadOnlyHeaders Headers { get; } = new Headers();
        public MessagingTransport Transport { get; set; } = null!;
        public ReceiveEndpoint Endpoint { get; set; } = null!;
        public string? MessageId { get; set; }
        public string? CorrelationId { get; set; }
        public string? ConversationId { get; set; }
        public string? CausationId { get; set; }
        public Uri? SourceAddress { get; set; }
        public Uri? DestinationAddress { get; set; }
        public Uri? ResponseAddress { get; set; }
        public Uri? FaultAddress { get; set; }
        public MessageContentType? ContentType { get; set; }
        public MessageType? MessageType { get; set; }
        public DateTimeOffset? SentAt { get; set; }
        public DateTimeOffset? DeliverBy { get; set; }
        public int? DeliveryCount { get; set; }
        public ReadOnlyMemory<byte> Body => ReadOnlyMemory<byte>.Empty;
        public MessageEnvelope? Envelope { get; set; }
        public IRemoteHostInfo Host { get; set; } = null!;
        public IMessagingRuntime Runtime { get; set; } = null!;
        public CancellationToken CancellationToken { get; set; }
        public IServiceProvider Services { get; set; } = null!;
    }
}
