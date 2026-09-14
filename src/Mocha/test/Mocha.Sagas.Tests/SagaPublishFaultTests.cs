using Microsoft.Extensions.DependencyInjection;
using Mocha.Events;
using Mocha.Transport.InMemory;

namespace Mocha.Sagas.Tests;

/// <summary>
/// Verifies that faults from events published by a saga are routed back to that saga.
/// </summary>
public class SagaPublishFaultTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    private static readonly TaskCompletionSource<NotAcknowledgedEvent> s_faultObserved = new();

    [Fact]
    public async Task Saga_Should_ReceiveFault_When_PublishedEventSubscriberFaults()
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

        // act
        await bus.PublishAsync(new StartPublishEvent(), CancellationToken.None);
        await handlerRan.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        var fault = await s_faultObserved.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        Assert.Equal(ErrorCodes.Exception, fault.ErrorCode);
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
