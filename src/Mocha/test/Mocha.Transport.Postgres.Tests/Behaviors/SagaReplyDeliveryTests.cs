using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Sagas;
using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests.Behaviors;

[Collection("Postgres")]
public sealed class SagaReplyDeliveryTests(PostgresFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task Saga_Should_CompleteOnAnotherInstance_When_SendingInstanceStopsBeforeReply()
    {
        // arrange
        await using var db = await fixture.CreateDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var sagaStates = new InMemorySagaStateStorage();
        var gate = new IdentityUserGate();

        await using var worker = await StartWorkerAsync(db.ConnectionString, sagaStates, gate);
        var instanceA = await StartSagaInstanceAsync(db.ConnectionString, sagaStates);
        await using var requester = await StartInstanceAsync(db.ConnectionString, sagaStates, _ => { });

        var request = SendProvisionUserAsync(requester, ct);
        await gate.Entered.Task.WaitAsync(s_timeout, ct);
        await using var instanceB = await StartSagaInstanceAsync(db.ConnectionString, sagaStates);

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

    [Fact]
    public async Task Saga_Should_CompleteOnAnotherInstance_When_SendingInstanceCrashesBeforeReply()
    {
        // arrange
        await using var db = await fixture.CreateDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var sagaStates = new InMemorySagaStateStorage();
        var gate = new IdentityUserGate();

        await using var worker = await StartWorkerAsync(db.ConnectionString, sagaStates, gate);
        var instanceA = await StartSagaInstanceAsync(db.ConnectionString, sagaStates);
        await using var requester = await StartInstanceAsync(db.ConnectionString, sagaStates, _ => { });

        var request = SendProvisionUserAsync(requester, ct);
        await gate.Entered.Task.WaitAsync(s_timeout, ct);
        await using var instanceB = await StartSagaInstanceAsync(db.ConnectionString, sagaStates);

        // act
        // a crash stops consuming and heartbeating but leaves the consumer row behind
        await CrashAsync(instanceA, ct);
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

        await instanceA.Provider.DisposeAsync();
    }

    [Fact]
    public async Task Saga_Should_ReceiveFaultOnAnotherInstance_When_SendingInstanceStopsBeforeFault()
    {
        // arrange
        await using var db = await fixture.CreateDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var sagaStates = new InMemorySagaStateStorage();
        var gate = new IdentityUserGate();

        await using var worker = await StartInstanceAsync(
            db.ConnectionString,
            sagaStates,
            b =>
            {
                b.Services.AddSingleton(gate);
                b.AddRequestHandler<GatedFailingCreateIdentityUserHandler>();
            });
        var instanceA = await StartInstanceAsync(
            db.ConnectionString,
            sagaStates,
            b => b.AddSaga<ProvisionUserWithFaultSaga>());
        await using var requester = await StartInstanceAsync(db.ConnectionString, sagaStates, _ => { });

        using var scope = requester.Provider.CreateScope();
        await scope.ServiceProvider
            .GetRequiredService<IMessageBus>()
            .SendAsync(new ProvisionUser("ada@example.com"), ct);
        await gate.Entered.Task.WaitAsync(s_timeout, ct);
        await using var instanceB = await StartInstanceAsync(
            db.ConnectionString,
            sagaStates,
            b => b.AddSaga<ProvisionUserWithFaultSaga>());

        // act
        await instanceA.DisposeAsync();
        gate.Release.SetResult();
        await WaitUntilAsync(() => sagaStates.Count == 0, ct);

        // assert
        Assert.Equal(0, sagaStates.Count);
    }

    private static Task<TestBus> StartWorkerAsync(
        string connectionString,
        InMemorySagaStateStorage sagaStates,
        IdentityUserGate gate)
        => StartInstanceAsync(
            connectionString,
            sagaStates,
            b =>
            {
                b.Services.AddSingleton(gate);
                b.AddRequestHandler<GatedCreateIdentityUserHandler>();
            });

    private static Task<TestBus> StartSagaInstanceAsync(
        string connectionString,
        InMemorySagaStateStorage sagaStates)
        => StartInstanceAsync(connectionString, sagaStates, b => b.AddSaga<ProvisionUserSaga>());

    private static async Task<TestBus> StartInstanceAsync(
        string connectionString,
        InMemorySagaStateStorage sagaStates,
        Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(sagaStates);
        services.AddInMemorySagas();

        var builder = services.AddMessageBus();
        configure(builder);

        return await builder.AddPostgres(t => t.ConnectionString(connectionString)).BuildTestBusAsync();
    }

    private static Task<UserProvisioned> SendProvisionUserAsync(TestBus requester, CancellationToken ct)
    {
        var scope = requester.Provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        return bus.RequestAsync(new ProvisionUser("ada@example.com"), ct).AsTask();
    }

    private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow + s_timeout;

        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(100, ct);
        }
    }

    private static async Task CrashAsync(TestBus instance, CancellationToken ct)
    {
        var runtime = (MessagingRuntime)instance.Provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        foreach (var endpoint in transport.ReceiveEndpoints)
        {
            await endpoint.StopAsync(runtime, ct);
        }

        await transport.DisposeAsync();
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

    public sealed class ProvisionUserWithFaultSaga : Saga<ProvisionUserState>
    {
        protected override void Configure(ISagaDescriptor<ProvisionUserState> descriptor)
        {
            descriptor
                .Initially()
                .OnRequest<ProvisionUser>()
                .StateFactory(r => new ProvisionUserState { Email = r.Email })
                .Send(s => new CreateIdentityUser(s.Id, s.Email))
                .TransitionTo("IdentityUserPending");

            descriptor.During("IdentityUserPending").OnFault().TransitionTo("Failed");

            descriptor.Finally("Failed");
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

    public sealed class GatedFailingCreateIdentityUserHandler(IdentityUserGate gate)
        : IEventRequestHandler<CreateIdentityUser, IdentityUserCreated>
    {
        public async ValueTask<IdentityUserCreated> HandleAsync(
            CreateIdentityUser request,
            CancellationToken cancellationToken)
        {
            gate.Entered.TrySetResult();
            await gate.Release.Task.WaitAsync(cancellationToken);
            throw new InvalidOperationException("The identity user could not be created.");
        }
    }
}
