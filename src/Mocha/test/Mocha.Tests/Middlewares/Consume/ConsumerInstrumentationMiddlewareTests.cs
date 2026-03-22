using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;
using Mocha.Middlewares;

namespace Mocha.Tests.Middlewares.Consume;

/// <summary>
/// Tests for <see cref="ConsumerInstrumentationMiddleware"/> which captures diagnostics
/// around consumer execution, separating handler-level work from transport-level work.
/// </summary>
public sealed class ConsumerInstrumentationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_Should_CallObserverConsume_When_MiddlewareInvoked()
    {
        // arrange
        var observer = new MockBusDiagnosticObserver();
        var middleware = new ConsumerInstrumentationMiddleware(observer);
        var context = new StubConsumeContext();
        var nextCalled = false;

        ConsumerDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.CompletedTask;
        };

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        Assert.True(observer.ConsumeCalled, "Observer.Consume should be called");
        Assert.True(nextCalled, "Next delegate should be called");
    }

    [Fact]
    public async Task InvokeAsync_Should_DisposeScope_When_ProcessingCompletes()
    {
        // arrange
        var scope = new MockDisposable();
        var observer = new MockBusDiagnosticObserver { ScopeToReturn = scope };
        var middleware = new ConsumerInstrumentationMiddleware(observer);
        var context = new StubConsumeContext();

        ConsumerDelegate next = _ => ValueTask.CompletedTask;

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        Assert.True(scope.WasDisposed, "Scope should be disposed after processing completes");
    }

    [Fact]
    public async Task InvokeAsync_Should_DisposeScope_When_ExceptionOccurs()
    {
        // arrange
        var scope = new MockDisposable();
        var observer = new MockBusDiagnosticObserver { ScopeToReturn = scope };
        var middleware = new ConsumerInstrumentationMiddleware(observer);
        var context = new StubConsumeContext();

        ConsumerDelegate next = _ => throw new InvalidOperationException("Test");

        // act
        try
        {
            await middleware.InvokeAsync(context, next);
        }
        catch (InvalidOperationException)
        {
            // expected
        }

        // assert - scope should be disposed even when exception occurs (using statement)
        Assert.True(scope.WasDisposed, "Scope should be disposed even when exception occurs");
    }

    [Fact]
    public async Task InvokeAsync_Should_RethrowException_When_NextThrows()
    {
        // arrange
        var observer = new MockBusDiagnosticObserver();
        var middleware = new ConsumerInstrumentationMiddleware(observer);
        var context = new StubConsumeContext();
        var expected = new InvalidOperationException("Should be rethrown");

        ConsumerDelegate next = _ => throw expected;

        // act & assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.InvokeAsync(context, next).AsTask()
        );

        Assert.Same(expected, ex);
    }

    [Fact]
    public async Task InvokeAsync_Should_PassContextToObserver_When_Invoked()
    {
        // arrange
        var observer = new MockBusDiagnosticObserver();
        var middleware = new ConsumerInstrumentationMiddleware(observer);
        var context = new StubConsumeContext();

        ConsumerDelegate next = _ => ValueTask.CompletedTask;

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        Assert.Same(context, observer.RecordedConsumeContext);
    }

    [Fact]
    public void Create_Should_ReturnConfiguration_WithCorrectKey()
    {
        // act
        var configuration = ConsumerInstrumentationMiddleware.Create();

        // assert
        Assert.NotNull(configuration);
        Assert.Equal("Instrumentation", configuration.Key);
        Assert.NotNull(configuration.Middleware);
    }

    [Fact]
    public async Task Create_Should_ProduceWorkingMiddleware_When_UsedWithServiceProvider()
    {
        // arrange
        var observer = new MockBusDiagnosticObserver();
        var services = new ServiceCollection();
        services.AddSingleton<IBusDiagnosticObserver>(observer);
        var provider = services.BuildServiceProvider();

        var configuration = ConsumerInstrumentationMiddleware.Create();
        var factoryContext = new ConsumerMiddlewareFactoryContext { Services = provider, Consumer = null! };
        var nextCalled = false;

        ConsumerDelegate terminalNext = _ =>
        {
            nextCalled = true;
            return ValueTask.CompletedTask;
        };

        // act
        var middlewareDelegate = configuration.Middleware(factoryContext, terminalNext);
        var consumeContext = new StubConsumeContext();
        await middlewareDelegate(consumeContext);

        // assert
        Assert.True(observer.ConsumeCalled);
        Assert.True(nextCalled);
    }

    private sealed class StubConsumeContext : IConsumeContext
    {
        public IFeatureCollection Features { get; } = new FeatureCollection();
        public IMessagingRuntime Runtime { get; set; } = null!;
        public CancellationToken CancellationToken { get; set; }
        public IServiceProvider Services { get; set; } = null!;
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
        public IReadOnlyHeaders Headers { get; } = new Headers();
        public DateTimeOffset? SentAt { get; set; }
        public DateTimeOffset? DeliverBy { get; set; }
        public int? DeliveryCount { get; set; }
        public ReadOnlyMemory<byte> Body => ReadOnlyMemory<byte>.Empty;
        public MessageEnvelope? Envelope { get; set; }
        public IRemoteHostInfo Host { get; set; } = null!;
    }

    private sealed class MockBusDiagnosticObserver : IBusDiagnosticObserver
    {
        public bool ConsumeCalled { get; private set; }
        public IConsumeContext? RecordedConsumeContext { get; private set; }
        public IDisposable? ScopeToReturn { get; set; }

        public IDisposable Dispatch(IDispatchContext context) => new MockDisposable();

        public IDisposable Receive(IReceiveContext context) => new MockDisposable();

        public IDisposable Consume(IConsumeContext context)
        {
            ConsumeCalled = true;
            RecordedConsumeContext = context;
            return ScopeToReturn ?? new MockDisposable();
        }

        public void OnReceiveError(IReceiveContext context, Exception exception) { }

        public void OnDispatchError(IDispatchContext context, Exception exception) { }

        public void OnConsumeError(IConsumeContext context, Exception exception) { }
    }

    private sealed class MockDisposable : IDisposable
    {
        public bool WasDisposed { get; private set; }

        public void Dispose() => WasDisposed = true;
    }
}
