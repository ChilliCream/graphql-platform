using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;

namespace Mocha.Tests.Middlewares.Dispatch;

/// <summary>
/// Tests for <see cref="DispatchInstrumentationMiddleware"/> which wraps dispatch execution
/// in diagnostic instrumentation and propagates activity metadata to outgoing headers.
/// </summary>
public sealed class DispatchInstrumentationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_Should_CallObserverDispatch_When_MiddlewareInvoked()
    {
        // arrange
        var events = new MockMessagingDiagnosticEvents();
        var middleware = new DispatchInstrumentationMiddleware(events);
        var context = new DispatchContext();
        var nextCalled = false;

        DispatchDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.CompletedTask;
        };

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        Assert.True(events.DispatchCalled, "Events.Dispatch should be called");
        Assert.True(nextCalled, "Next delegate should be called");
    }

    [Fact]
    public async Task InvokeAsync_Should_DisposeActivity_When_ProcessingCompletes()
    {
        // arrange
        var activity = new MockDisposable();
        var events = new MockMessagingDiagnosticEvents { ActivityToReturn = activity };
        var middleware = new DispatchInstrumentationMiddleware(events);
        var context = new DispatchContext();

        DispatchDelegate next = _ => ValueTask.CompletedTask;

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        Assert.True(activity.WasDisposed, "Activity should be disposed after processing completes");
    }

    [Fact]
    public async Task InvokeAsync_Should_DisposeActivity_When_ExceptionOccurs()
    {
        // arrange
        var activity = new MockDisposable();
        var events = new MockMessagingDiagnosticEvents { ActivityToReturn = activity };
        var middleware = new DispatchInstrumentationMiddleware(events);
        var context = new DispatchContext();

        DispatchDelegate next = _ => throw new InvalidOperationException("Test");

        // act
        try
        {
            await middleware.InvokeAsync(context, next);
        }
        catch (InvalidOperationException)
        {
            // expected
        }

        // assert - activity should be disposed even when exception occurs (using statement)
        Assert.True(activity.WasDisposed, "Activity should be disposed even when exception occurs");
    }

    [Fact]
    public async Task InvokeAsync_Should_RethrowException_When_NextThrows()
    {
        // arrange
        var events = new MockMessagingDiagnosticEvents();
        var middleware = new DispatchInstrumentationMiddleware(events);
        var context = new DispatchContext();
        var expected = new InvalidOperationException("Should be rethrown");

        DispatchDelegate next = _ => throw expected;

        // act & assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.InvokeAsync(context, next).AsTask()
        );

        Assert.Same(expected, ex);
    }

    [Fact]
    public async Task InvokeAsync_Should_CallDispatchError_When_ExceptionOccurs()
    {
        // arrange
        var events = new MockMessagingDiagnosticEvents();
        var middleware = new DispatchInstrumentationMiddleware(events);
        var context = new DispatchContext();
        var expectedException = new InvalidOperationException("Test exception");

        DispatchDelegate next = _ => throw expectedException;

        // act & assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.InvokeAsync(context, next).AsTask()
        );

        Assert.Same(expectedException, ex);
        Assert.True(events.DispatchErrorCalled, "DispatchError should be called on exception");
        Assert.Same(expectedException, events.RecordedException);
        Assert.Same(context, events.RecordedErrorContext);
    }

    [Fact]
    public async Task InvokeAsync_Should_PassContextToObserver_When_Invoked()
    {
        // arrange
        var events = new MockMessagingDiagnosticEvents();
        var middleware = new DispatchInstrumentationMiddleware(events);
        var context = new DispatchContext();

        DispatchDelegate next = _ => ValueTask.CompletedTask;

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        Assert.Same(context, events.RecordedDispatchContext);
    }

    [Fact]
    public async Task InvokeAsync_Should_CallWithActivityOnHeaders_When_Invoked()
    {
        // arrange - WithActivity() is a no-op when Activity.Current is null,
        // but we verify the middleware calls next and does not throw.
        var events = new MockMessagingDiagnosticEvents();
        var middleware = new DispatchInstrumentationMiddleware(events);
        var context = new DispatchContext();

        DispatchDelegate next = _ => ValueTask.CompletedTask;

        // act - should not throw even without an active Activity
        await middleware.InvokeAsync(context, next);

        // assert
        Assert.True(events.DispatchCalled);
    }

    [Fact]
    public void Create_Should_ReturnConfiguration_WithCorrectKey()
    {
        // act
        var configuration = DispatchInstrumentationMiddleware.Create();

        // assert
        Assert.NotNull(configuration);
        Assert.Equal("Instrumentation", configuration.Key);
        Assert.NotNull(configuration.Middleware);
    }

    [Fact]
    public async Task Create_Should_ProduceWorkingMiddleware_When_UsedWithServiceProvider()
    {
        // arrange
        var events = new MockMessagingDiagnosticEvents();
        var services = new ServiceCollection();
        services.AddSingleton<IMessagingDiagnosticEvents>(events);
        var provider = services.BuildServiceProvider();

        var configuration = DispatchInstrumentationMiddleware.Create();
        var factoryContext = new DispatchMiddlewareFactoryContext
        {
            Services = provider,
            Endpoint = null!,
            Transport = null!
        };
        var nextCalled = false;

        DispatchDelegate terminalNext = _ =>
        {
            nextCalled = true;
            return ValueTask.CompletedTask;
        };

        // act
        var middlewareDelegate = configuration.Middleware(factoryContext, terminalNext);
        var dispatchContext = new DispatchContext();
        await middlewareDelegate(dispatchContext);

        // assert
        Assert.True(events.DispatchCalled);
        Assert.True(nextCalled);
    }

    private sealed class MockMessagingDiagnosticEvents : IMessagingDiagnosticEvents
    {
        public bool DispatchCalled { get; private set; }
        public bool DispatchErrorCalled { get; private set; }
        public IDispatchContext? RecordedDispatchContext { get; private set; }
        public IDispatchContext? RecordedErrorContext { get; private set; }
        public Exception? RecordedException { get; private set; }
        public IDisposable? ActivityToReturn { get; set; }

        public IDisposable Dispatch(IDispatchContext context)
        {
            DispatchCalled = true;
            RecordedDispatchContext = context;
            return ActivityToReturn ?? new MockDisposable();
        }

        public void DispatchError(IDispatchContext context, Exception exception)
        {
            DispatchErrorCalled = true;
            RecordedErrorContext = context;
            RecordedException = exception;
        }

        public IDisposable Receive(IReceiveContext context) => new MockDisposable();

        public void ReceiveError(IReceiveContext context, Exception exception) { }

        public IDisposable Consume(IConsumeContext context) => new MockDisposable();

        public void ConsumeError(IConsumeContext context, Exception exception) { }
    }

    private sealed class MockDisposable : IDisposable
    {
        public bool WasDisposed { get; private set; }

        public void Dispose() => WasDisposed = true;
    }
}
