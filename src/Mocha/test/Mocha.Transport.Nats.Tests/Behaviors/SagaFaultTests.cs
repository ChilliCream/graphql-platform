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

    [Fact]
    public async Task Saga_Should_ReceiveFault_When_ItsSentRequestFailsTerminally()
    {
        // arrange
        // The fault reply crosses the transport twice: the request goes out over JetStream, and the
        // fault comes back to the saga's reply subject over core NATS, correlated by the saga
        // header. Without it the saga would wait in its send state until it timed out.
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

        // assert
        await handlerFaulted.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        var fault = await FaultSaga.Observed.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        Assert.Equal(ErrorCodes.Exception, fault.ErrorCode);
    }

    public sealed class FaultState : SagaStateBase;

    public sealed class FaultSaga : Saga<FaultState>
    {
        public static TaskCompletionSource<NotAcknowledgedEvent> Observed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

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
                .Then((_, fault) => Observed.TrySetResult(fault))
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
