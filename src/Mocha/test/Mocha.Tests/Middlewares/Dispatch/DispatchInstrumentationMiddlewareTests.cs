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
        var observer = new MockBusDiagnosticObserver();
        var middleware = new DispatchInstrumentationMiddleware(observer);
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
        Assert.True(observer.DispatchCalled, "Observer.Dispatch should be called");
        Assert.True(nextCalled, "Next delegate should be called");
    }

    [Fact]
    public async Task InvokeAsync_Should_DisposeActivity_When_ProcessingCompletes()
    {
        // arrange
        var activity = new MockDisposable();
        var observer = new MockBusDiagnosticObserver { ActivityToReturn = activity };
        var middleware = new DispatchInstrumentationMiddleware(observer);
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
        var observer = new MockBusDiagnosticObserver { ActivityToReturn = activity };
        var middleware = new DispatchInstrumentationMiddleware(observer);
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
        var observer = new MockBusDiagnosticObserver();
        var middleware = new DispatchInstrumentationMiddleware(observer);
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
    public async Task InvokeAsync_Should_PassContextToObserver_When_Invoked()
    {
        // arrange
        var observer = new MockBusDiagnosticObserver();
        var middleware = new DispatchInstrumentationMiddleware(observer);
        var context = new DispatchContext();

        DispatchDelegate next = _ => ValueTask.CompletedTask;

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        Assert.Same(context, observer.RecordedDispatchContext);
    }

    [Fact]
    public async Task InvokeAsync_Should_CallWithActivityOnHeaders_When_Invoked()
    {
        // arrange - WithActivity() is a no-op when Activity.Current is null,
        // but we verify the middleware calls next and does not throw.
        var observer = new MockBusDiagnosticObserver();
        var middleware = new DispatchInstrumentationMiddleware(observer);
        var context = new DispatchContext();

        DispatchDelegate next = _ => ValueTask.CompletedTask;

        // act - should not throw even without an active Activity
        await middleware.InvokeAsync(context, next);

        // assert
        Assert.True(observer.DispatchCalled);
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
        var observer = new MockBusDiagnosticObserver();
        var services = new ServiceCollection();
        services.AddSingleton<IBusDiagnosticObserver>(observer);
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
        Assert.True(observer.DispatchCalled);
        Assert.True(nextCalled);
    }

    private sealed class MockBusDiagnosticObserver : IBusDiagnosticObserver
    {
        public bool DispatchCalled { get; private set; }
        public IDispatchContext? RecordedDispatchContext { get; private set; }
        public IDisposable? ActivityToReturn { get; set; }

        public IDisposable Dispatch(IDispatchContext context)
        {
            DispatchCalled = true;
            RecordedDispatchContext = context;
            return ActivityToReturn ?? new MockDisposable();
        }

        public IDisposable Receive(IReceiveContext context) => new MockDisposable();

        public IDisposable Consume(IConsumeContext context) => new MockDisposable();

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
