using Microsoft.Extensions.DependencyInjection;
using Mocha.Events;
using Mocha.Features;
using Mocha.Middlewares;
using Mocha.Transport.InMemory;

namespace Mocha.Sagas.Tests;

/// <summary>
/// Characterizes the publish leg of saga fault handling. A published event carries no reply address,
/// so a failing subscriber is routed to the error endpoint rather than back to the saga.
/// </summary>
public class SagaPublishFaultTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    private static readonly TaskCompletionSource<NotAcknowledgedEvent> s_faultObserved = new();

    [Fact]
    public async Task Saga_Should_NotReceiveFault_When_PublishedEventSubscriberFaults()
    {
        // arrange
        var handlerRan = new TaskCompletionSource();
        var services = new ServiceCollection();
        services.AddInMemorySagas();
        var builder = services.AddMessageBus();
        builder.Services.AddSingleton(handlerRan);
        builder.AddEventHandler<FaultingSubscriber>();
        builder.AddSaga<PublishingSaga>();
        builder.AddInMemory();

        await using var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var storage = provider.GetRequiredService<InMemorySagaStateStorage>();

        // act
        await bus.PublishAsync(new StartPublishEvent(), CancellationToken.None);

        // assert - the subscriber ran and threw, proving the published event was delivered
        await handlerRan.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert - the saga is never told, so it stays in its waiting state holding its state
        var completed = await Task.WhenAny(s_faultObserved.Task, Task.Delay(2000, TestContext.Current.CancellationToken));
        Assert.NotSame(s_faultObserved.Task, completed);
        Assert.Equal(1, storage.Count);
    }

    [Fact]
    public async Task ErrorEndpoint_Should_ReceiveSagaId_When_PublishedEventSubscriberFaults()
    {
        // The fault carries the original envelope, so the saga header survives onto the error
        // endpoint even though the saga itself is never notified.

        // arrange
        var handlerRan = new TaskCompletionSource();
        var services = new ServiceCollection();
        services.AddInMemorySagas();
        var builder = services.AddMessageBus();
        builder.Services.AddSingleton(handlerRan);
        builder.AddEventHandler<ErrorQueueSubscriber>();
        builder.AddSaga<ErrorQueuePublishingSaga>();
        builder.AddInMemory(d => d.AddConvention(new TestErrorEndpointConvention()));

        await using var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new StartErrorQueueEvent(), CancellationToken.None);
        await handlerRan.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        var envelope = await ReadFirstErrorEnvelopeAsync(runtime);
        Assert.Equal(MessageKind.Fault, envelope.Headers!.Get(MessageHeaders.MessageKind));
        Assert.True(Guid.TryParse(envelope.Headers!.Get(SagaContextData.SagaId), out _));
    }

    /// <summary>
    /// Drains whichever error queue receives a message first, since every default endpoint gets one.
    /// </summary>
    private static async Task<MessageEnvelope> ReadFirstErrorEnvelopeAsync(MessagingRuntime runtime)
    {
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = (InMemoryMessagingTopology)transport.Topology;
        var errorQueues = topology.Queues.Where(q => q.Name.EndsWith("_error")).ToArray();

        using var cts = new CancellationTokenSource(s_timeout);

        var reads = errorQueues
            .Select(async queue =>
            {
                await foreach (var item in queue.ConsumeAsync(cts.Token))
                {
                    var envelope = new MessageEnvelope(item.Envelope);
                    item.Dispose();
                    return envelope;
                }

                return null;
            })
            .ToArray();

        var completed = await Task.WhenAny(reads);
        await cts.CancelAsync();

        return await completed ?? throw new InvalidOperationException("no error envelope was received");
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
                var feature = configuration.Features.GetOrSet<ReceiveFaultEndpointFeature>();
                feature.Address ??= new Uri($"{transport.Schema}:q/{queueName}_error");
            }
        }
    }

    public sealed class StartErrorQueueEvent;

    public sealed record ErrorQueueEvent(Guid Id);

    public sealed class ErrorQueueSubscriber(TaskCompletionSource handlerRan) : IEventHandler<ErrorQueueEvent>
    {
        public ValueTask HandleAsync(ErrorQueueEvent message, CancellationToken cancellationToken)
        {
            handlerRan.TrySetResult();
            throw new InvalidOperationException("terminal failure");
        }
    }

    public sealed class ErrorQueuePublishingSaga : Saga<PublishState>
    {
        protected override void Configure(ISagaDescriptor<PublishState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<StartErrorQueueEvent>()
                .StateFactory(_ => new PublishState())
                .Publish((_, state) => new ErrorQueueEvent(state.Id))
                .TransitionTo("Awaiting");

            descriptor.During("Awaiting").OnFault().TransitionTo("Failed");

            descriptor.Finally("Failed");
        }
    }

    public sealed class PublishState : SagaStateBase;

    public sealed class StartPublishEvent;

    public sealed record PublishedEvent(Guid Id);

    public sealed class FaultingSubscriber(TaskCompletionSource handlerRan) : IEventHandler<PublishedEvent>
    {
        public ValueTask HandleAsync(PublishedEvent message, CancellationToken cancellationToken)
        {
            handlerRan.TrySetResult();
            throw new InvalidOperationException("terminal failure");
        }
    }

    public sealed class PublishingSaga : Saga<PublishState>
    {
        protected override void Configure(ISagaDescriptor<PublishState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<StartPublishEvent>()
                .StateFactory(_ => new PublishState())
                .Publish((_, state) => new PublishedEvent(state.Id))
                .TransitionTo("Awaiting");

            descriptor
                .During("Awaiting")
                .OnFault()
                .Then((_, fault) => s_faultObserved.TrySetResult(fault))
                .TransitionTo("Failed");

            descriptor.Finally("Failed");
        }
    }
}
