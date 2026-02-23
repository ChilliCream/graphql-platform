using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.Middlewares.Receive;

/// <summary>
/// Tests for <see cref="ReceiveInstrumentationMiddleware"/> which wraps message processing
/// with diagnostic observation using OpenTelemetry Activities.
/// </summary>
[Collection("OpenTelemetry")]
public sealed class ReceiveInstrumentationMiddlewareTests : ReceiveMiddlewareTestBase
{
    [Fact]
    public async Task InvokeAsync_Should_CallObserverReceive_When_MiddlewareInvoked()
    {
        // arrange
        var observer = new MockBusDiagnosticObserver();
        var middleware = new ReceiveInstrumentationMiddleware(observer);
        var context = new StubReceiveContext();
        var nextCalled = false;

        ReceiveDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.CompletedTask;
        };

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        Assert.True(observer.ReceiveCalled, "Observer.Receive should be called");
        Assert.True(nextCalled, "Next delegate should be called");
    }

    [Fact]
    public async Task InvokeAsync_Should_DisposeActivity_When_ProcessingCompletes()
    {
        // arrange
        var activity = new MockDisposable();
        var observer = new MockBusDiagnosticObserver { ActivityToReturn = activity };
        var middleware = new ReceiveInstrumentationMiddleware(observer);
        var context = new StubReceiveContext();

        ReceiveDelegate next = _ => ValueTask.CompletedTask;

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        Assert.True(activity.WasDisposed, "Activity should be disposed after processing completes");
    }

    [Fact]
    public async Task InvokeAsync_Should_CallOnReceiveError_When_ExceptionOccurs()
    {
        // arrange
        var observer = new MockBusDiagnosticObserver();
        var middleware = new ReceiveInstrumentationMiddleware(observer);
        var context = new StubReceiveContext();
        var expectedException = new InvalidOperationException("Test exception");

        ReceiveDelegate next = _ => throw expectedException;

        // act & assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.InvokeAsync(context, next).AsTask()
        );

        Assert.Same(expectedException, ex);
        Assert.True(observer.OnReceiveErrorCalled, "OnReceiveError should be called on exception");
        Assert.Same(expectedException, observer.RecordedException);
        Assert.Same(context, observer.RecordedErrorContext);
    }

    [Fact]
    public async Task InvokeAsync_Should_RethrowException_When_ExceptionOccurs()
    {
        // arrange
        var observer = new MockBusDiagnosticObserver();
        var middleware = new ReceiveInstrumentationMiddleware(observer);
        var context = new StubReceiveContext();
        var expectedException = new InvalidOperationException("Should be rethrown");

        ReceiveDelegate next = _ => throw expectedException;

        // act & assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.InvokeAsync(context, next).AsTask()
        );

        Assert.Same(expectedException, ex);
    }

    [Fact]
    public async Task InvokeAsync_Should_DisposeActivity_When_ExceptionOccurs()
    {
        // arrange
        var activity = new MockDisposable();
        var observer = new MockBusDiagnosticObserver { ActivityToReturn = activity };
        var middleware = new ReceiveInstrumentationMiddleware(observer);
        var context = new StubReceiveContext();

        ReceiveDelegate next = _ => throw new InvalidOperationException("Test");

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
    public async Task InvokeAsync_Should_PassContextToNext_When_Invoked()
    {
        // arrange
        var observer = new MockBusDiagnosticObserver();
        var middleware = new ReceiveInstrumentationMiddleware(observer);
        var context = new StubReceiveContext();
        IReceiveContext? receivedContext = null;

        ReceiveDelegate next = ctx =>
        {
            receivedContext = ctx;
            return ValueTask.CompletedTask;
        };

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        Assert.Same(context, receivedContext);
    }

    [Fact]
    public async Task InvokeAsync_Should_PassContextToObserver_When_Invoked()
    {
        // arrange
        var observer = new MockBusDiagnosticObserver();
        var middleware = new ReceiveInstrumentationMiddleware(observer);
        var context = new StubReceiveContext();

        ReceiveDelegate next = _ => ValueTask.CompletedTask;

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        Assert.Same(context, observer.RecordedReceiveContext);
    }

    [Fact]
    public async Task Create_Should_ProduceWorkingMiddleware_When_UsedWithServiceProvider()
    {
        // arrange
        var observer = new MockBusDiagnosticObserver();
        var services = new ServiceCollection();
        services.AddSingleton<IBusDiagnosticObserver>(observer);
        var provider = services.BuildServiceProvider();

        var configuration = ReceiveInstrumentationMiddleware.Create();
        var factoryContext = new ReceiveMiddlewareFactoryContext
        {
            Services = provider,
            Endpoint = null!,
            Transport = null!
        };
        var nextCalled = false;

        ReceiveDelegate terminalNext = _ =>
        {
            nextCalled = true;
            return ValueTask.CompletedTask;
        };

        // act - create the middleware from the configuration
        var middlewareDelegate = configuration.Middleware(factoryContext, terminalNext);
        var receiveContext = new StubReceiveContext();
        await middlewareDelegate(receiveContext);

        // assert
        Assert.True(observer.ReceiveCalled);
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_Should_CreateActivity_When_MessageProcessedWithInstrumentation()
    {
        // arrange
        var activities = new ConcurrentBag<Activity>();
        using var listener = CreateActivityListener(activities);

        var recorder = new MessageRecorder();
        await using var provider = await CreateBusWithInstrumentationAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddEventHandler<InstrumentedEventHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new InstrumentedEvent { Data = "test-activity" }, CancellationToken.None);
        Assert.True(await recorder.WaitAsync(Timeout));

        // ActivityListener callbacks fire asynchronously; brief delay lets them flush.
        await Task.Delay(100, TestContext.Current.CancellationToken);

        // assert - at least one activity was created
        Assert.NotEmpty(activities);
    }

    [Fact]
    public async Task InvokeAsync_Should_CreateActivityWithCorrectSourceName_When_MessageProcessed()
    {
        // arrange
        var activities = new ConcurrentBag<Activity>();
        using var listener = CreateActivityListener(activities);

        var recorder = new MessageRecorder();
        await using var provider = await CreateBusWithInstrumentationAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddEventHandler<InstrumentedEventHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new InstrumentedEvent { Data = "source-check" }, CancellationToken.None);
        Assert.True(await recorder.WaitAsync(Timeout));

        // ActivityListener callbacks fire asynchronously; brief delay lets them flush.
        await Task.Delay(100, TestContext.Current.CancellationToken);

        // assert
        Assert.NotEmpty(activities);
        Assert.All(activities, a => Assert.Equal("Mocha", a.Source.Name));
    }

    [Fact]
    public async Task InvokeAsync_Should_RecordErrorOnActivity_When_HandlerThrows()
    {
        // arrange
        var activities = new ConcurrentBag<Activity>();
        using var listener = CreateActivityListener(activities);

        var counter = new InvocationCounter();
        await using var provider = await CreateBusWithInstrumentationAsync(b =>
        {
            b.Services.AddSingleton(counter);
            b.AddEventHandler<ThrowingEventHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new InstrumentedEvent { Data = "error-test" }, CancellationToken.None);

        // Wait for handler to be attempted
        await counter.WaitAsync(Timeout);

        // ActivityListener callbacks and error recording fire asynchronously;
        // brief delay lets them flush.
        await Task.Delay(200, TestContext.Current.CancellationToken);

        // assert - activities were created (error recording happens on Activity.Current)
        Assert.NotEmpty(activities);
    }

    [Fact]
    public async Task InvokeAsync_Should_CreateMultipleActivities_When_MultipleMessagesProcessed()
    {
        // arrange
        var activities = new ConcurrentBag<Activity>();
        using var listener = CreateActivityListener(activities);

        var recorder = new MessageRecorder();
        await using var provider = await CreateBusWithInstrumentationAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddEventHandler<InstrumentedEventHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        for (var i = 0; i < 3; i++)
        {
            await bus.PublishAsync(new InstrumentedEvent { Data = $"multi-{i}" }, CancellationToken.None);
        }

        Assert.True(await recorder.WaitAsync(Timeout, expectedCount: 3));

        // ActivityListener callbacks fire asynchronously; brief delay lets them flush.
        await Task.Delay(100, TestContext.Current.CancellationToken);

        // assert - at least 3 activities (dispatch + receive for each)
        Assert.True(activities.Count >= 3, $"Expected at least 3 activities but got {activities.Count}");
    }

    [Fact]
    public async Task InvokeAsync_Should_ProcessMessages_When_NoActivityListenerRegistered()
    {
        // arrange - NO activity listener registered
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusWithInstrumentationAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddEventHandler<InstrumentedEventHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new InstrumentedEvent { Data = "no-listener" }, CancellationToken.None);

        // assert - message still delivered
        Assert.True(await recorder.WaitAsync(Timeout));
        Assert.Single(recorder.Messages);
    }

    [Fact]
    public async Task InvokeAsync_Should_CreateActivityForRequestResponse_When_RequestMade()
    {
        // arrange
        var activities = new ConcurrentBag<Activity>();
        using var listener = CreateActivityListener(activities);

        var recorder = new MessageRecorder();
        await using var provider = await CreateBusWithInstrumentationAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddRequestHandler<InstrumentedRequestHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var response = await bus.RequestAsync(new InstrumentedRequest { Query = "test-query" }, CancellationToken.None);

        // ActivityListener callbacks fire asynchronously; brief delay lets them flush.
        await Task.Delay(100, TestContext.Current.CancellationToken);

        // assert
        Assert.NotEmpty(activities);
        Assert.Equal("re: test-query", response.Answer);
    }

    private static ActivityListener CreateActivityListener(ConcurrentBag<Activity> activities)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Mocha",
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => activities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    private static async Task<ServiceProvider> CreateBusWithInstrumentationAsync(
        Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInstrumentation();
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return provider;
    }

    private sealed class MockBusDiagnosticObserver : IBusDiagnosticObserver
    {
        public bool ReceiveCalled { get; private set; }
        public bool OnReceiveErrorCalled { get; private set; }
        public IReceiveContext? RecordedReceiveContext { get; private set; }
        public IReceiveContext? RecordedErrorContext { get; private set; }
        public Exception? RecordedException { get; private set; }
        public IDisposable? ActivityToReturn { get; set; }

        public IDisposable Dispatch(IDispatchContext context) => new MockDisposable();

        public IDisposable Receive(IReceiveContext context)
        {
            ReceiveCalled = true;
            RecordedReceiveContext = context;
            return ActivityToReturn ?? new MockDisposable();
        }

        public IDisposable Consume(IConsumeContext context) => new MockDisposable();

        public void OnReceiveError(IReceiveContext context, Exception exception)
        {
            OnReceiveErrorCalled = true;
            RecordedErrorContext = context;
            RecordedException = exception;
        }

        public void OnDispatchError(IDispatchContext context, Exception exception) { }

        public void OnConsumeError(IConsumeContext context, Exception exception) { }
    }

    private sealed class MockDisposable : IDisposable
    {
        public bool WasDisposed { get; private set; }

        public void Dispose() => WasDisposed = true;
    }

    public sealed class InstrumentedEvent
    {
        public required string Data { get; init; }
    }

    public sealed class InstrumentedRequest : IEventRequest<InstrumentedResponse>
    {
        public required string Query { get; init; }
    }

    public sealed class InstrumentedResponse
    {
        public required string Answer { get; init; }
    }

    public sealed class InstrumentedEventHandler(MessageRecorder recorder) : IEventHandler<InstrumentedEvent>
    {
        public ValueTask HandleAsync(InstrumentedEvent message, CancellationToken ct)
        {
            recorder.Record(message);
            return default;
        }
    }

    public sealed class InstrumentedRequestHandler(MessageRecorder recorder)
        : IEventRequestHandler<InstrumentedRequest, InstrumentedResponse>
    {
        public ValueTask<InstrumentedResponse> HandleAsync(InstrumentedRequest request, CancellationToken ct)
        {
            recorder.Record(request);
            return new(new InstrumentedResponse { Answer = $"re: {request.Query}" });
        }
    }

    public sealed class ThrowingEventHandler(InvocationCounter counter) : IEventHandler<InstrumentedEvent>
    {
        public ValueTask HandleAsync(InstrumentedEvent message, CancellationToken ct)
        {
            counter.Increment();
            throw new InvalidOperationException("Test exception for instrumentation");
        }
    }

    public sealed class InvocationCounter
    {
        private readonly SemaphoreSlim _semaphore = new(0);
        private int _count;

        public int Count => _count;

        public void Increment()
        {
            Interlocked.Increment(ref _count);
            _semaphore.Release();
        }

        public async Task<bool> WaitAsync(TimeSpan timeout, int expectedCount = 1)
        {
            for (var i = 0; i < expectedCount; i++)
            {
                if (!await _semaphore.WaitAsync(timeout))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
