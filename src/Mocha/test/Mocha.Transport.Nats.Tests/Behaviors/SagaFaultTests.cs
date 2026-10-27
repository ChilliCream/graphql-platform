using Microsoft.Extensions.DependencyInjection;
using Mocha.Events;
using Mocha.Sagas;
using Mocha.Transport.Nats.Tests.Fixtures;
using Mocha.Transport.Nats.Tests.Helpers;

namespace Mocha.Transport.Nats.Tests.Behaviors;

[Collection(JetStreamCollection.Name)]
public sealed class SagaFaultTests(JetStreamFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    private static readonly TaskCompletionSource<NotAcknowledgedEvent> s_faultObserved =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    [Fact]
    public async Task Saga_Should_ReceiveFault_When_ItsSentRequestFailsTerminally()
    {
        // arrange
        await using var scope = await fixture.CreateScopeAsync();
        var handlerFaulted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(handlerFaulted)
            .AddInMemorySagas()
            .AddMessageBus()
            .AddRequestHandler<FaultingRequestHandler>()
            .AddSaga<FaultSaga>()
            .AddResilience(policy => policy.Default().DeadLetter())
            .AddNats(nats => nats.StreamName(scope.StreamName))
            .BuildTestBusAsync();

        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;

        // act
        await messageBus.PublishAsync(new StartFault(), TestContext.Current.CancellationToken);
        await handlerFaulted.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        var fault = await s_faultObserved.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        Assert.Equal(ErrorCodes.Exception, fault.ErrorCode);
    }

    public sealed class FaultState : SagaStateBase;

    public sealed class FaultSaga : Saga<FaultState>
    {
        protected override void Configure(ISagaDescriptor<FaultState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<StartFault>()
                .StateFactory(_ => new FaultState())
                .Send((_, _) => new FaultingRequest())
                .TransitionTo("AwaitingResponse");

            descriptor
                .During("AwaitingResponse")
                .OnFault()
                .Then((_, fault) => s_faultObserved.TrySetResult(fault))
                .TransitionTo("Failed");

            descriptor.Finally("Failed");
        }
    }

    public sealed class StartFault;

    public sealed record FaultingResponse;

    public sealed record FaultingRequest : IEventRequest<FaultingResponse>;

    public sealed class FaultingRequestHandler(TaskCompletionSource handlerFaulted)
        : IEventRequestHandler<FaultingRequest, FaultingResponse>
    {
        public ValueTask<FaultingResponse> HandleAsync(FaultingRequest request, CancellationToken cancellationToken)
        {
            handlerFaulted.TrySetResult();
            throw new InvalidOperationException("terminal failure");
        }
    }
}
