using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Sagas;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection("RabbitMQ")]
public sealed class SagaReplyDeliveryTests(RabbitMQFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task Saga_Should_CompleteOnAnotherInstance_When_SendingInstanceStopsBeforeReply()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var ct = TestContext.Current.CancellationToken;
        var sagaStates = new InMemorySagaStateStorage();
        var gate = new IdentityUserGate();

        await using var worker = await StartInstanceAsync(
            vhost,
            sagaStates,
            b =>
            {
                b.Services.AddSingleton(gate);
                b.AddRequestHandler<GatedCreateIdentityUserHandler>();
            });
        var instanceA = await StartInstanceAsync(vhost, sagaStates, b => b.AddSaga<ProvisionUserSaga>());
        await using var requester = await StartInstanceAsync(vhost, sagaStates, _ => { });

        using var scope = requester.Provider.CreateScope();
        var request = scope.ServiceProvider
            .GetRequiredService<IMessageBus>()
            .RequestAsync(new ProvisionUser("ada@example.com"), ct)
            .AsTask();
        await gate.Entered.Task.WaitAsync(s_timeout, ct);
        await using var instanceB = await StartInstanceAsync(vhost, sagaStates, b => b.AddSaga<ProvisionUserSaga>());

        // act
        await instanceA.DisposeAsync();
        gate.Release.SetResult();
        var response = await request.WaitAsync(s_timeout, ct);

        // assert
        new { response.Email, PersistedSagas = sagaStates.Count }.MatchInlineSnapshot(
            """
            {
              "Email": "ada@example.com",
              "PersistedSagas": 0
            }
            """);
    }

    private static async Task<TestBus> StartInstanceAsync(
        VhostContext vhost,
        InMemorySagaStateStorage sagaStates,
        Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(vhost.ConnectionFactory);
        services.AddSingleton(sagaStates);
        services.AddInMemorySagas();

        var builder = services.AddMessageBus();
        configure(builder);

        return await builder.AddRabbitMQ().BuildTestBusAsync();
    }

    public sealed record ProvisionUser(string Email) : IEventRequest<UserProvisioned>;

    public sealed record UserProvisioned(Guid SagaId, string Email);

    public sealed record CreateIdentityUser(Guid SagaId, string Email) : IEventRequest<IdentityUserCreated>;

    public sealed record IdentityUserCreated(Guid SagaId);

    public sealed class ProvisionUserState : SagaStateBase
    {
        public required string Email { get; init; }
    }

    public sealed class ProvisionUserSaga : Saga<ProvisionUserState>
    {
        protected override void Configure(ISagaDescriptor<ProvisionUserState> descriptor)
        {
            descriptor
                .Initially()
                .OnRequest<ProvisionUser>()
                .StateFactory(r => new ProvisionUserState { Email = r.Email })
                .Send(s => new CreateIdentityUser(s.Id, s.Email))
                .TransitionTo("IdentityUserPending");

            descriptor
                .During("IdentityUserPending")
                .OnReply<IdentityUserCreated>()
                .TransitionTo("Success");

            descriptor.Finally("Success").Respond(s => new UserProvisioned(s.Id, s.Email));
        }
    }

    public sealed class IdentityUserGate
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class GatedCreateIdentityUserHandler(IdentityUserGate gate)
        : IEventRequestHandler<CreateIdentityUser, IdentityUserCreated>
    {
        public async ValueTask<IdentityUserCreated> HandleAsync(
            CreateIdentityUser request,
            CancellationToken cancellationToken)
        {
            gate.Entered.TrySetResult();
            await gate.Release.Task.WaitAsync(cancellationToken);
            return new IdentityUserCreated(request.SagaId);
        }
    }
}
