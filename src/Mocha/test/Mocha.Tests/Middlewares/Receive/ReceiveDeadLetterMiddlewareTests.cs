using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Mocha.Features;
using Mocha.Middlewares;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.Middlewares.Receive;

/// <summary>
/// Tests for <see cref="ReceiveDeadLetterMiddleware"/>.
/// </summary>
public sealed class ReceiveDeadLetterMiddlewareTests : ReceiveMiddlewareTestBase
{
    [Fact]
    public async Task InvokeAsync_Should_CallNext_When_Invoked()
    {
        // arrange
        var tracker = new InvocationTracker();
        var (middleware, _) = CreateMiddleware();
        var context = new StubReceiveContext { Services = CreateServices(), Runtime = new StubMessagingRuntime() };
        var next = CreateTrackingDelegate(tracker);

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        Assert.True(tracker.WasInvoked);
    }

    [Fact]
    public async Task InvokeAsync_Should_CatchException_And_NotPropagate()
    {
        // arrange
        var (middleware, _) = CreateMiddleware();
        var context = new StubReceiveContext { Services = CreateServices(), Runtime = new StubMessagingRuntime() };
        var next = CreateThrowingDelegate(new InvalidOperationException("boom"));

        // act - dead letter's job IS to swallow handler exceptions and route to
        // the error endpoint, so "did not throw" is the primary assertion.
        await middleware.InvokeAsync(context, next);

        // assert - the middleware must also mark the message as consumed
        // so downstream middleware does not re-process it.
        var feature = context.Features.GetOrSet<ReceiveConsumerFeature>();
        Assert.True(feature.MessageConsumed);
    }

    [Fact]
    public async Task InvokeAsync_Should_SkipDeadLetter_When_MessageConsumed()
    {
        // arrange
        var (middleware, pools) = CreateMiddleware();
        var context = new StubReceiveContext { Services = CreateServices(), Runtime = new StubMessagingRuntime() };
        var next = CreateConsumingDelegate();

        // act
        await middleware.InvokeAsync(context, next);

        // assert - pool should not have been accessed
        Assert.Equal(0, pools.GetCount);
    }

    [Fact]
    public async Task InvokeAsync_Should_DispatchToErrorEndpoint_When_MessageNotConsumed()
    {
        // arrange
        var executed = false;
        var (middleware, _) = CreateMiddleware(onExecute: _ =>
        {
            executed = true;
            return ValueTask.CompletedTask;
        });
        var context = new StubReceiveContext
        {
            Services = CreateServices(),
            Runtime = new StubMessagingRuntime(),
            Envelope = CreateEnvelope()
        };
        var next = CreatePassthroughDelegate();

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        Assert.True(executed);
    }

    [Fact]
    public async Task InvokeAsync_Should_CopyEnvelopeToDispatchContext()
    {
        // arrange
        var envelope = CreateEnvelope(messageId: "envelope-1");
        MessageEnvelope? capturedEnvelope = null;
        var (middleware, _) = CreateMiddleware(onExecute: ctx =>
        {
            capturedEnvelope = ctx.Envelope;
            return ValueTask.CompletedTask;
        });
        var context = new StubReceiveContext
        {
            Services = CreateServices(),
            Runtime = new StubMessagingRuntime(),
            Envelope = envelope
        };
        var next = CreatePassthroughDelegate();

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        Assert.NotNull(capturedEnvelope);
        Assert.Same(envelope, capturedEnvelope);
    }

    [Fact]
    public async Task InvokeAsync_Should_SetMessageConsumed_After_DeadLetterDispatch()
    {
        // arrange
        var (middleware, _) = CreateMiddleware();
        var context = new StubReceiveContext
        {
            Services = CreateServices(),
            Runtime = new StubMessagingRuntime(),
            Envelope = CreateEnvelope()
        };
        var next = CreatePassthroughDelegate();

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        var feature = context.Features.GetOrSet<ReceiveConsumerFeature>();
        Assert.True(feature.MessageConsumed);
    }

    [Fact]
    public async Task InvokeAsync_Should_ReturnDispatchContextToPool()
    {
        // arrange
        var (middleware, pools) = CreateMiddleware();
        var context = new StubReceiveContext
        {
            Services = CreateServices(),
            Runtime = new StubMessagingRuntime(),
            Envelope = CreateEnvelope()
        };
        var next = CreatePassthroughDelegate();

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        Assert.Equal(1, pools.GetCount);
        Assert.Equal(1, pools.ReturnCount);
    }

    [Fact]
    public async Task InvokeAsync_Should_ReturnDispatchContextToPool_When_ExecuteAsyncThrows()
    {
        // arrange
        var (middleware, pools) = CreateMiddleware(onExecute: _ =>
            throw new InvalidOperationException("dispatch failure")
        );
        var context = new StubReceiveContext
        {
            Services = CreateServices(),
            Runtime = new StubMessagingRuntime(),
            Envelope = CreateEnvelope()
        };
        var next = CreatePassthroughDelegate();

        // act & assert - the exception from ExecuteAsync propagates
        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context, next).AsTask());

        // assert - pool Return was still called (finally block)
        Assert.Equal(1, pools.GetCount);
        Assert.Equal(1, pools.ReturnCount);
    }

    [Fact]
    public async Task InvokeAsync_Should_DeliverMessage_When_HandlerRegistered()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddEventHandler<DeadLetterTestEventHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new DeadLetterTestEvent { Id = "normal-1" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(Timeout), "Handler should be called for a normal message");

        var message = Assert.Single(recorder.Messages);
        var evt = Assert.IsType<DeadLetterTestEvent>(message);
        Assert.Equal("normal-1", evt.Id);
    }

    [Fact]
    public async Task InvokeAsync_Should_NotInterfereWithHandler_When_MessageConsumedSuccessfully()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddEventHandler<DeadLetterTestEventHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish two messages in sequence
        await bus.PublishAsync(new DeadLetterTestEvent { Id = "consumed-1" }, CancellationToken.None);
        await bus.PublishAsync(new DeadLetterTestEvent { Id = "consumed-2" }, CancellationToken.None);

        // assert - both messages should be received by the handler
        Assert.True(
            await recorder.WaitAsync(Timeout, expectedCount: 2),
            "Handler should receive both messages without dead letter interference");

        Assert.Equal(2, recorder.Messages.Count);
        var ids = recorder.Messages.Cast<DeadLetterTestEvent>().Select(e => e.Id).ToList();
        Assert.Contains("consumed-1", ids);
        Assert.Contains("consumed-2", ids);
    }

    [Fact]
    public async Task InvokeAsync_Should_CatchException_When_HandlerThrows()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddEventHandler<DeadLetterThrowingEventHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish a message whose handler throws; dead letter catches it
        await bus.PublishAsync(new DeadLetterTestEvent { Id = "throws-1" }, CancellationToken.None);

        // No deterministic signal for a swallowed exception; let the fault settle
        // so the message is routed to the error endpoint.
        await Task.Delay(500, default);

        // assert - the recorder should NOT have the message because the handler threw
        // before it could record. Dead letter middleware catches the exception silently.
        Assert.Empty(recorder.Messages);
    }

    [Fact]
    public async Task InvokeAsync_Should_StillProcessNextMessage_When_PreviousHandlerThrew()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.Services.AddSingleton<DeadLetterConditionalThrowHandler>();
            b.AddEventHandler<DeadLetterConditionalThrowHandler>();
        });

        var handler = provider.GetRequiredService<DeadLetterConditionalThrowHandler>();
        handler.ThrowForIds.Add("fail-1");

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - first message throws, second should succeed
        await bus.PublishAsync(new DeadLetterTestEvent { Id = "fail-1" }, CancellationToken.None);

        // Let the fault propagate before publishing the next message -
        // no deterministic signal for a swallowed exception.
        await Task.Delay(200, default);

        await bus.PublishAsync(new DeadLetterTestEvent { Id = "success-1" }, CancellationToken.None);

        // assert - only the successful message should be recorded
        Assert.True(
            await recorder.WaitAsync(Timeout),
            "Handler should still process subsequent messages after a failure");

        var recorded = Assert.Single(recorder.Messages);
        Assert.Equal("success-1", ((DeadLetterTestEvent)recorded).Id);
    }

    [Fact]
    public async Task InvokeAsync_Should_RecordOnlySuccessful_When_MixedSuccessAndFailure()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.Services.AddSingleton<DeadLetterConditionalThrowHandler>();
            b.AddEventHandler<DeadLetterConditionalThrowHandler>();
        });

        var handler = provider.GetRequiredService<DeadLetterConditionalThrowHandler>();
        handler.ThrowForIds.Add("bad-1");
        handler.ThrowForIds.Add("bad-3");

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish 5 messages, 2 will fail (bad-1, bad-3)
        await bus.PublishAsync(new DeadLetterTestEvent { Id = "good-1" }, CancellationToken.None);
        await bus.PublishAsync(new DeadLetterTestEvent { Id = "bad-1" }, CancellationToken.None);
        await bus.PublishAsync(new DeadLetterTestEvent { Id = "good-2" }, CancellationToken.None);
        await bus.PublishAsync(new DeadLetterTestEvent { Id = "bad-3" }, CancellationToken.None);
        await bus.PublishAsync(new DeadLetterTestEvent { Id = "good-3" }, CancellationToken.None);

        // assert - only the 3 successful messages should be recorded
        Assert.True(
            await recorder.WaitAsync(Timeout, expectedCount: 3),
            "Should receive exactly 3 successful messages");

        // Negative wait: confirm no extra messages arrive after the expected 3.
        await Task.Delay(200, default);

        Assert.Equal(3, recorder.Messages.Count);
        var ids = recorder.Messages.Cast<DeadLetterTestEvent>().Select(e => e.Id).OrderBy(id => id).ToList();
        Assert.Contains("good-1", ids);
        Assert.Contains("good-2", ids);
        Assert.Contains("good-3", ids);
        Assert.DoesNotContain("bad-1", ids);
        Assert.DoesNotContain("bad-3", ids);
    }

    [Fact]
    public async Task InvokeAsync_Should_DeliverToErrorEndpoint_When_HandlerThrows()
    {
        // arrange - with an error endpoint convention, the fault middleware (inside
        // the dead letter) sends faulted messages to the error queue. Dead letter
        // sees MessageConsumed = true and does not re-forward.
        await using var provider = await CreateBusWithErrorEndpointAsync(b =>
            b.AddEventHandler<DeadLetterThrowingEventHandler>());

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new DeadLetterTestEvent { Id = "dl-err-1" }, CancellationToken.None);

        // assert - one message on the error queue with fault headers
        var errorQueue = GetErrorQueue(provider);
        var items = await ConsumeFromQueueAsync(errorQueue, expectedCount: 1);

        var envelope = Assert.Single(items);
        Assert.Equal(MessageKind.Fault, envelope.Headers!.Get(MessageHeaders.MessageKind));
        Assert.Contains("InvalidOperationException", envelope.Headers!.Get(MessageHeaders.Fault.ExceptionType));
    }

    [Fact]
    public async Task InvokeAsync_Should_DeliverAllFaultsToErrorEndpoint_When_MultipleHandlersThrow()
    {
        // arrange
        await using var provider = await CreateBusWithErrorEndpointAsync(b =>
            b.AddEventHandler<DeadLetterThrowingEventHandler>());

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        for (var i = 0; i < 3; i++)
        {
            await bus.PublishAsync(new DeadLetterTestEvent { Id = $"dl-multi-{i}" }, CancellationToken.None);
        }

        // assert
        var errorQueue = GetErrorQueue(provider);
        var items = await ConsumeFromQueueAsync(errorQueue, expectedCount: 3);

        Assert.Equal(3, items.Count);
        Assert.All(
            items,
            envelope =>
                Assert.Equal(MessageKind.Fault, envelope.Headers!.Get(MessageHeaders.MessageKind)));
    }

    [Fact]
    public async Task InvokeAsync_Should_OnlyDeliverFaults_When_MixedSuccessAndFailure()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusWithErrorEndpointAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.Services.AddSingleton<DeadLetterConditionalThrowHandler>();
            b.AddEventHandler<DeadLetterConditionalThrowHandler>();
        });

        var handler = provider.GetRequiredService<DeadLetterConditionalThrowHandler>();
        handler.ThrowForIds.Add("dl-fail-1");
        handler.ThrowForIds.Add("dl-fail-2");

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - 5 messages: 3 succeed, 2 fail
        await bus.PublishAsync(new DeadLetterTestEvent { Id = "dl-ok-1" }, CancellationToken.None);
        await bus.PublishAsync(new DeadLetterTestEvent { Id = "dl-fail-1" }, CancellationToken.None);
        await bus.PublishAsync(new DeadLetterTestEvent { Id = "dl-ok-2" }, CancellationToken.None);
        await bus.PublishAsync(new DeadLetterTestEvent { Id = "dl-fail-2" }, CancellationToken.None);
        await bus.PublishAsync(new DeadLetterTestEvent { Id = "dl-ok-3" }, CancellationToken.None);

        // assert - 3 successes recorded by handler
        Assert.True(
            await recorder.WaitAsync(Timeout, expectedCount: 3),
            "Should receive exactly 3 successful messages");
        Assert.Equal(3, recorder.Messages.Count);

        // assert - 2 faults on error queue
        var errorQueue = GetErrorQueue(provider);
        var items = await ConsumeFromQueueAsync(errorQueue, expectedCount: 2);

        Assert.Equal(2, items.Count);
        Assert.All(
            items,
            envelope =>
                Assert.Equal(MessageKind.Fault, envelope.Headers!.Get(MessageHeaders.MessageKind)));
    }

    private static async Task<ServiceProvider> CreateBusWithErrorEndpointAsync(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory(d => d.AddConvention(new TestErrorEndpointConvention()));

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return provider;
    }

    private static InMemoryQueue GetErrorQueue(ServiceProvider provider)
    {
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = (InMemoryMessagingTopology)transport.Topology;
        return topology.Queues.First(q => q.Name.EndsWith("_error"));
    }

    private static async Task<List<MessageEnvelope>> ConsumeFromQueueAsync(InMemoryQueue queue, int expectedCount)
    {
        using var cts = new CancellationTokenSource(Timeout);
        var items = new List<MessageEnvelope>();

        try
        {
            await foreach (var item in queue.ConsumeAsync(cts.Token))
            {
                items.Add(new MessageEnvelope(item.Envelope));
                item.Dispose();
                if (items.Count == expectedCount)
                {
                    cts.Cancel();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when we cancel after reading all messages.
        }

        return items;
    }

    private sealed class TestErrorEndpointConvention : IInMemoryReceiveEndpointConfigurationConvention
    {
        public void Configure(
            IMessagingConfigurationContext context,
            InMemoryMessagingTransport transport,
            InMemoryReceiveEndpointConfiguration configuration)
        {
            if (configuration is { Kind: ReceiveEndpointKind.Default, QueueName: { } queueName })
            {
                configuration.ErrorEndpoint ??= new Uri($"{transport.Schema}:q/{queueName}_error");
            }
        }
    }

    private static (ReceiveDeadLetterMiddleware middleware, MockMessagingPools pools) CreateMiddleware(
        Func<IDispatchContext, ValueTask>? onExecute = null)
    {
        var transportOptions = new StubTransportOptions();
        var transport = new StubTransport();
        SetTransportOptions(transport, transportOptions);

        var endpoint = new StubDispatchEndpoint(transport);
        SetPipeline(endpoint, onExecute ?? (_ => ValueTask.CompletedTask));

        var dispatchContext = new DispatchContext();
        var pools = new MockMessagingPools(dispatchContext);
        var logger = NullLogger<ReceiveDeadLetterMiddleware>.Instance;

        var middleware = new ReceiveDeadLetterMiddleware(endpoint, pools, logger);
        return (middleware, pools);
    }

    private static void SetPipeline(DispatchEndpoint endpoint, Func<IDispatchContext, ValueTask> handler)
    {
        var field = typeof(DispatchEndpoint).GetField("_pipeline", BindingFlags.NonPublic | BindingFlags.Instance)!;
        DispatchDelegate pipeline = ctx => handler(ctx);
        field.SetValue(endpoint, pipeline);
    }

    private static void SetTransportOptions(MessagingTransport transport, IReadOnlyTransportOptions options)
    {
        var prop = typeof(MessagingTransport).GetProperty(
            nameof(MessagingTransport.Options),
            BindingFlags.Public | BindingFlags.Instance)!;
        prop.SetValue(transport, options);
    }

    private sealed class StubTransportOptions : IReadOnlyTransportOptions
    {
        public MessageContentType? DefaultContentType => null;
        public IReadOnlyTransportCircuitBreakerOptions CircuitBreaker => null!;
    }

    private sealed class StubMessagingOptions : IReadOnlyMessagingOptions
    {
        public MessageContentType DefaultContentType => new("application/json");
    }

    private sealed class StubHostInfo : IHostInfo
    {
        public string MachineName => "test-machine";
        public string ProcessName => "test-process";
        public int ProcessId => 1;
        public string? AssemblyName => "test";
        public string? AssemblyVersion => "1.0.0";
        public string? PackageVersion => "1.0.0";
        public string FrameworkVersion => ".NET 9.0";
        public string OperatingSystemVersion => "Linux";
        public string EnvironmentName => "Test";
        public string? ServiceName => "test-service";
        public string? ServiceVersion => "1.0.0";
        public Guid InstanceId => Guid.Empty;
        public IRuntimeInfo RuntimeInfo => null!;
    }

    private sealed class StubMessagingRuntime : IMessagingRuntime
    {
        public IHostInfo Host { get; } = new StubHostInfo();
        public IReadOnlyMessagingOptions Options { get; } = new StubMessagingOptions();
        public IServiceProvider Services => null!;
        public IBusNamingConventions Naming => null!;
        public IMessageTypeRegistry Messages => null!;
        public IMessageRouter Router => null!;
        public IEndpointRouter Endpoints => null!;
        public IConventionRegistry Conventions => null!;
        public ImmutableHashSet<Consumer> Consumers => [];
        public ImmutableArray<MessagingTransport> Transports => [];
        public IFeatureCollection Features => null!;
        public IMessageBusTopology Topology => null!;

        public DispatchEndpoint GetSendEndpoint(MessageType messageType) => null!;

        public DispatchEndpoint GetPublishEndpoint(MessageType messageType) => null!;

        public DispatchEndpoint GetDispatchEndpoint(Uri address) => null!;

        public MessageType GetMessageType(Type type) => null!;

        public MessageType? GetMessageType(string? identity) => null;

        public MessagingTransport? GetTransport(Uri address) => null;
    }

    private sealed class StubTransport : MessagingTransport
    {
        public override MessagingTopology Topology => null!;

        public override bool TryGetDispatchEndpoint(Uri address, [NotNullWhen(true)] out DispatchEndpoint? endpoint)
        {
            endpoint = null;
            return false;
        }

        public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
            IMessagingConfigurationContext context,
            OutboundRoute route)
            => null;

        public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
            IMessagingConfigurationContext context,
            Uri address)
            => null;

        public override ReceiveEndpointConfiguration? CreateEndpointConfiguration(
            IMessagingConfigurationContext context,
            InboundRoute route)
            => null;

        protected override MessagingTransportConfiguration CreateConfiguration(IMessagingSetupContext context) => null!;

        protected override ReceiveEndpoint CreateReceiveEndpoint() => null!;

        protected override DispatchEndpoint CreateDispatchEndpoint() => null!;
    }

    private sealed class StubDispatchEndpoint : DispatchEndpoint
    {
        public StubDispatchEndpoint(MessagingTransport transport) : base(transport) { }

        protected override void OnInitialize(
            IMessagingConfigurationContext context,
            DispatchEndpointConfiguration configuration)
        { }

        protected override void OnComplete(
            IMessagingConfigurationContext context,
            DispatchEndpointConfiguration configuration)
        { }

        protected override ValueTask DispatchAsync(IDispatchContext context) => ValueTask.CompletedTask;
    }

    private sealed class MockMessagingPools : IMessagingPools
    {
        public int GetCount;
        public int ReturnCount;

        public ObjectPool<DispatchContext> DispatchContext { get; }
        public ObjectPool<ReceiveContext> ReceiveContext => null!;

        public MockMessagingPools(DispatchContext instance)
        {
            DispatchContext = new SimpleObjectPool<DispatchContext>(instance, this);
        }
    }

    private sealed class SimpleObjectPool<T>(T instance, MockMessagingPools tracker) : ObjectPool<T> where T : class
    {
        public override T Get()
        {
            Interlocked.Increment(ref tracker.GetCount);
            return instance;
        }

        public override void Return(T obj)
        {
            Interlocked.Increment(ref tracker.ReturnCount);
        }
    }

    public sealed class DeadLetterTestEvent
    {
        public required string Id { get; init; }
    }

    public sealed class DeadLetterTestEventHandler(MessageRecorder recorder) : IEventHandler<DeadLetterTestEvent>
    {
        public ValueTask HandleAsync(DeadLetterTestEvent message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    public sealed class DeadLetterThrowingEventHandler : IEventHandler<DeadLetterTestEvent>
    {
        public ValueTask HandleAsync(DeadLetterTestEvent message, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException($"Simulated failure for {message.Id}");
        }
    }

    public sealed class DeadLetterConditionalThrowHandler(MessageRecorder recorder) : IEventHandler<DeadLetterTestEvent>
    {
        public ConcurrentBag<string> ThrowForIds { get; } = [];

        public ValueTask HandleAsync(DeadLetterTestEvent message, CancellationToken cancellationToken)
        {
            if (ThrowForIds.Contains(message.Id))
            {
                throw new InvalidOperationException($"Configured to throw for {message.Id}");
            }

            recorder.Record(message);
            return default;
        }
    }
}
