using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Sagas.Tests;

public class SagaReplyEndpointTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Saga_Should_ReceiveReplyOnRequestTransport_When_HandlerIsOnAnotherTransport()
    {
        // arrange
        var recorder = new ResponseAddressRecorder();
        var services = new ServiceCollection();
        services.AddSingleton(recorder);
        services.AddInMemorySagas();
        services
            .AddMessageBus()
            .AddSaga<LookupSaga>()
            .AddRequestHandler<LookupHandler>()
            .AddInMemory(t =>
            {
                t.Name("default");
                t.IsDefaultTransport();
            })
            .AddInMemory(t =>
            {
                t.Name("secondary");
                t.Schema("secondary");
                t.BindExplicitly();
                t.Queue("lookup").Handler<LookupHandler>();
                t.DispatchEndpoint("lookup").ToQueue("lookup").Send<Lookup>();
            });

        await using var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus
            .RequestAsync(new StartLookup(), CancellationToken.None)
            .AsTask()
            .WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        recorder.Address?.ToString().MatchInlineSnapshot(
            "secondary://mocha.sagas.tests/q/mocha.sagas.tests.lookup-saga_reply");
    }

    public sealed class LookupState : SagaStateBase;

    public sealed record StartLookup : IEventRequest<LookupCompleted>;

    public sealed record LookupCompleted;

    public sealed record Lookup : IEventRequest<LookupResult>;

    public sealed record LookupResult;

    public sealed class ResponseAddressRecorder
    {
        public Uri? Address { get; set; }
    }

    public sealed class LookupHandler(ConsumeContextAccessor accessor, ResponseAddressRecorder recorder)
        : IEventRequestHandler<Lookup, LookupResult>
    {
        public ValueTask<LookupResult> HandleAsync(Lookup request, CancellationToken cancellationToken)
        {
            recorder.Address = accessor.Context?.ResponseAddress;
            return new(new LookupResult());
        }
    }

    public sealed class LookupSaga : Saga<LookupState>
    {
        protected override void Configure(ISagaDescriptor<LookupState> descriptor)
        {
            descriptor
                .Initially()
                .OnRequest<StartLookup>()
                .StateFactory(_ => new LookupState())
                .Send((_, _) => new Lookup())
                .TransitionTo("Awaiting");

            descriptor.During("Awaiting").OnReply<LookupResult>().TransitionTo("Done");

            descriptor.Finally("Done").Respond(_ => new LookupCompleted());
        }
    }
}
