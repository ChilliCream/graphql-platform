using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.Middlewares;

/// <summary>
/// Tests that the messages a consumer dispatches leave the bus once the consumer attempt has
/// completed, and that dispatches outside that rule keep leaving at once.
/// </summary>
public sealed class DeferredDispatchTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task SendAsync_Should_LeaveAfterConsumerCompletes_When_CalledFromConsumer()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var probe = new DispatchProbe();
        await using var provider = await CreateBusAsync(
            b =>
            {
                b.Services.AddSingleton(probe);
                b.AddEventHandler<SendingHandler>();
                b.AddRequestHandler<HeldCommandHandler>();
            },
            // queues process in order, so a probe sent after the held command is handled after it
            t => t.AddConvention(new SingleWorkerConvention()));

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new StartSend(), ct);
        await probe.Sent.Task.WaitAsync(s_timeout, ct);
        await bus.SendAsync(new HeldCommand("probe"), ct);
        await probe.ProbeHandled.Task.WaitAsync(s_timeout, ct);
        var handledWhileConsumerParked = probe.CommandHandled.Task.IsCompleted;
        probe.Release.SetResult();
        await probe.CommandHandled.Task.WaitAsync(s_timeout, ct);

        // assert
        Assert.False(handledWhileConsumerParked, "the command left before the consumer completed");
        Assert.True(probe.ConsumerCompletedBeforeCommandHandled);
    }

    [Fact]
    public async Task PublishAsync_Should_LeaveOnce_When_ConsumerThrowsAndIsRetried()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var probe = new DispatchProbe();
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(probe);
            b.Services.AddSingleton(recorder);
            b.AddResilience(p => p
                .On<InvalidOperationException>()
                .Retry(2, TimeSpan.Zero, RetryBackoffType.Constant));
            b.AddRequestHandler<PublishThenThrowHandler>();
            b.AddEventHandler<RecordingHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        // the acknowledgement leaves after the event of the attempt that succeeded
        await bus.RequestAsync(new StartPublish(), ct);
        var received = await recorder.WaitAsync(s_timeout);

        // assert
        Assert.True(received);
        Assert.Equal(2, probe.Attempts);
        Assert.Single(recorder.Messages);
    }

    [Fact]
    public async Task SendAsync_Should_LeaveBeforeConsumerCompletes_When_ImmediateDispatchIsSet()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var probe = new DispatchProbe();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(probe);
            b.AddEventHandler<ImmediateSendingHandler>();
            b.AddRequestHandler<HeldCommandHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new StartImmediateSend(), ct);
        await probe.Completed.Task.WaitAsync(s_timeout, ct);

        // assert
        Assert.False(probe.ConsumerCompletedBeforeCommandHandled);
    }

    [Fact]
    public async Task RequestAsync_Should_Complete_When_CalledFromConsumer()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var probe = new DispatchProbe();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(probe);
            b.AddEventHandler<RequestingHandler>();
            b.AddRequestHandler<EchoHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new StartRequest("ada"), ct);
        var response = await probe.Response.Task.WaitAsync(s_timeout, ct);

        // assert
        Assert.Equal("ada", response.Value);
    }

    [Fact]
    public async Task Consume_Should_ReleaseMessagesPerConsumer_When_EndpointHasTwoConsumers()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var probe = new DispatchProbe();
        await using var provider = await CreateBusAsync(
            b =>
            {
                b.Services.AddSingleton(probe);
                b.AddEventHandler<FirstSharedHandler>();
                b.AddEventHandler<SecondSharedHandler>();
                b.AddRequestHandler<OrderedCommandHandler>();
            },
            t => t.Queue("shared").Handler<FirstSharedHandler>().Handler<SecondSharedHandler>());

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new StartShared(), ct);
        await probe.SecondCommandHandled.Task.WaitAsync(s_timeout, ct);

        // assert
        Assert.Equal(["first", "second"], probe.HandledCommands);
    }

    [Fact]
    public async Task SendAsync_Should_LeaveAtOnce_When_CalledFromReceiveMiddleware()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddEventHandler<StartMarkerHandler>();
            b.AddRequestHandler<MarkerHandler>();
            b.ConfigureMessageBus(x => x.UseReceive(SendAfterConsumeMiddleware.Create()));
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new StartMarker(), ct);
        var received = await recorder.WaitAsync(s_timeout);

        // assert
        Assert.True(received, "the marker sent from the receive middleware never left");
        Assert.IsType<Marker>(Assert.Single(recorder.Messages));
    }

    private static async Task<ServiceProvider> CreateBusAsync(
        Action<IMessageBusHostBuilder> configure,
        Action<IInMemoryMessagingTransportDescriptor>? configureTransport = null)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory(configureTransport ?? (static _ => { }));

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return provider;
    }

    public sealed class DispatchProbe
    {
        private int _attempts;

        private int _sharedConsumers;

        public TaskCompletionSource Sent { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource CommandHandled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ProbeHandled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource FirstCommandHandled { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource SecondCommandHandled { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<Echoed> Response { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ConcurrentQueue<string> HandledCommands { get; } = new();

        public bool ConsumerCompleted { get; set; }

        public bool ConsumerCompletedBeforeCommandHandled { get; set; }

        public int Attempts => _attempts;

        public int NextAttempt() => Interlocked.Increment(ref _attempts);

        public int NextSharedConsumer() => Interlocked.Increment(ref _sharedConsumers);
    }

    public sealed record StartSend;

    public sealed record StartImmediateSend;

    public sealed record HeldCommand(string Name);

    public sealed record StartPublish;

    public sealed record HeldEvent;

    public sealed record StartRequest(string Value);

    public sealed record Echo(string Value) : IEventRequest<Echoed>;

    public sealed record Echoed(string Value);

    public sealed record StartShared;

    public sealed record OrderedCommand(string Name);

    public sealed record StartMarker;

    public sealed record Marker;

    public sealed class SendingHandler(IMessageBus bus, DispatchProbe probe) : IEventHandler<StartSend>
    {
        public async ValueTask HandleAsync(StartSend message, CancellationToken cancellationToken)
        {
            await bus.SendAsync(new HeldCommand("held"), cancellationToken);
            probe.Sent.TrySetResult();
            await probe.Release.Task.WaitAsync(s_timeout, cancellationToken);
            probe.ConsumerCompleted = true;
        }
    }

    public sealed class ImmediateSendingHandler(IMessageBus bus, DispatchProbe probe)
        : IEventHandler<StartImmediateSend>
    {
        public async ValueTask HandleAsync(StartImmediateSend message, CancellationToken cancellationToken)
        {
            await bus.SendAsync(
                new HeldCommand("held"),
                new SendOptions { ImmediateDispatch = true },
                cancellationToken);

            try
            {
                // a held command cannot arrive while the consumer that sent it is still running
                await probe.CommandHandled.Task.WaitAsync(s_timeout, cancellationToken);
                probe.Completed.TrySetResult();
            }
            catch (Exception ex)
            {
                probe.Completed.TrySetException(ex);
                throw;
            }
        }
    }

    public sealed class HeldCommandHandler(DispatchProbe probe) : IEventRequestHandler<HeldCommand>
    {
        public ValueTask HandleAsync(HeldCommand request, CancellationToken cancellationToken)
        {
            if (request.Name == "probe")
            {
                probe.ProbeHandled.TrySetResult();
                return default;
            }

            probe.ConsumerCompletedBeforeCommandHandled = probe.ConsumerCompleted;
            probe.CommandHandled.TrySetResult();
            return default;
        }
    }

    public sealed class PublishThenThrowHandler(IMessageBus bus, DispatchProbe probe)
        : IEventRequestHandler<StartPublish>
    {
        public async ValueTask HandleAsync(StartPublish request, CancellationToken cancellationToken)
        {
            await bus.PublishAsync(new HeldEvent(), cancellationToken);

            if (probe.NextAttempt() == 1)
            {
                throw new InvalidOperationException("the first attempt fails after publishing");
            }
        }
    }

    public sealed class RecordingHandler(MessageRecorder recorder) : IEventHandler<HeldEvent>
    {
        public ValueTask HandleAsync(HeldEvent message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    public sealed class RequestingHandler(IMessageBus bus, DispatchProbe probe) : IEventHandler<StartRequest>
    {
        public async ValueTask HandleAsync(StartRequest message, CancellationToken cancellationToken)
        {
            try
            {
                var response = await bus.RequestAsync(new Echo(message.Value), cancellationToken);
                probe.Response.TrySetResult(response);
            }
            catch (Exception ex)
            {
                probe.Response.TrySetException(ex);
                throw;
            }
        }
    }

    public sealed class EchoHandler : IEventRequestHandler<Echo, Echoed>
    {
        public ValueTask<Echoed> HandleAsync(Echo request, CancellationToken cancellationToken)
            => new(new Echoed(request.Value));
    }

    public sealed class FirstSharedHandler(IMessageBus bus, DispatchProbe probe) : IEventHandler<StartShared>
    {
        public ValueTask HandleAsync(StartShared message, CancellationToken cancellationToken)
            => SendInOrderAsync(bus, probe, cancellationToken);
    }

    public sealed class SecondSharedHandler(IMessageBus bus, DispatchProbe probe) : IEventHandler<StartShared>
    {
        public ValueTask HandleAsync(StartShared message, CancellationToken cancellationToken)
            => SendInOrderAsync(bus, probe, cancellationToken);
    }

    /// <summary>
    /// The consumers of one endpoint run in no fixed order, so whichever runs first sends the first
    /// command, and the other one waits for that command to arrive before it sends its own.
    /// </summary>
    private static async ValueTask SendInOrderAsync(IMessageBus bus, DispatchProbe probe, CancellationToken ct)
    {
        if (probe.NextSharedConsumer() == 1)
        {
            await bus.SendAsync(new OrderedCommand("first"), ct);
            return;
        }

        // the first consumer's command left when its attempt completed, before this consumer started
        await probe.FirstCommandHandled.Task.WaitAsync(s_timeout, ct);
        await bus.SendAsync(new OrderedCommand("second"), ct);
    }

    public sealed class OrderedCommandHandler(DispatchProbe probe) : IEventRequestHandler<OrderedCommand>
    {
        public ValueTask HandleAsync(OrderedCommand request, CancellationToken cancellationToken)
        {
            probe.HandledCommands.Enqueue(request.Name);

            if (request.Name == "first")
            {
                probe.FirstCommandHandled.TrySetResult();
            }
            else
            {
                probe.SecondCommandHandled.TrySetResult();
            }

            return default;
        }
    }

    public sealed class StartMarkerHandler : IEventHandler<StartMarker>
    {
        public ValueTask HandleAsync(StartMarker message, CancellationToken cancellationToken) => default;
    }

    public sealed class MarkerHandler(MessageRecorder recorder) : IEventRequestHandler<Marker>
    {
        public ValueTask HandleAsync(Marker request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return default;
        }
    }

    /// <summary>
    /// Processes every queue with one worker, so the messages of a queue are handled in the order
    /// they arrived.
    /// </summary>
    private sealed class SingleWorkerConvention : IInMemoryReceiveEndpointConfigurationConvention
    {
        public void Configure(
            IMessagingConfigurationContext context,
            InMemoryMessagingTransport transport,
            InMemoryReceiveEndpointConfiguration configuration)
        {
            configuration.MaxConcurrency = 1;
        }
    }

    /// <summary>
    /// Sends a marker from the receive scope after the consumers of a start marker ran. The receive
    /// scope holds nothing, so the marker leaves at once.
    /// </summary>
    private static class SendAfterConsumeMiddleware
    {
        public static ReceiveMiddlewareConfiguration Create()
            => new(
                static (_, next) => async context =>
                {
                    await next(context);

                    if (context.MessageType?.RuntimeType == typeof(StartMarker))
                    {
                        var bus = context.Services.GetRequiredService<IMessageBus>();
                        await bus.SendAsync(new Marker(), context.CancellationToken);
                    }
                },
                "SendAfterConsume");
    }
}
