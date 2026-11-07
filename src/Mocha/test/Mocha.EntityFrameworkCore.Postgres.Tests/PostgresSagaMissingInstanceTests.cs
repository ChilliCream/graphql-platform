using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Inbox;
using Mocha.Sagas;
using Mocha.Transport.InMemory;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

/// <summary>
/// Tests that a saga event faulting on a missing saga instance leaves neither saga state nor an
/// inbox claim behind, so a retry reprocesses it once the instance is persisted.
/// </summary>
public sealed class PostgresSagaMissingInstanceTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(15);

    [Fact]
    public async Task Saga_Should_ReprocessEventOnRetry_When_InboxClaimedTheFailedAttempt()
    {
        // arrange
        var orderId = Guid.NewGuid();
        var attempts = new AttemptRecorder();
        await using var host = await StartHostAsync(attempts);

        using var scope = host.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        // the confirmation overtakes the event that starts the saga
        await bus.PublishAsync(new OrderConfirmed(orderId), TestContext.Current.CancellationToken);
        await attempts.WaitForAttemptAsync();
        var afterFault = await ReadStoreAsync(host);

        await bus.PublishAsync(new OrderPlaced(orderId), TestContext.Current.CancellationToken);
        await attempts.WaitForAttemptAsync();
        var afterStart = await ReadStoreAsync(host);

        attempts.ReleaseFailedAttempt();
        await attempts.WaitForAttemptAsync();
        var afterRetry = await ReadStoreAsync(host);

        // assert
        Assert.Equal(
            [
                "OrderConfirmed: SagaInstanceNotFoundException",
                "OrderPlaced: Consumed",
                "OrderConfirmed: Consumed"
            ],
            attempts.Attempts);
        Assert.Equal(new StoreRows(InboxClaims: 0, SagaStates: 0), afterFault);
        Assert.Equal(new StoreRows(InboxClaims: 1, SagaStates: 1), afterStart);
        Assert.Equal(new StoreRows(InboxClaims: 2, SagaStates: 0), afterRetry);
    }

    private static async Task<StoreRows> ReadStoreAsync(MessagingTestHost host)
    {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        return new StoreRows(
            await db.Set<InboxMessage>().CountAsync(TestContext.Current.CancellationToken),
            await db.Set<Mocha.Sagas.EfCore.SagaState>().CountAsync(TestContext.Current.CancellationToken));
    }

    private async Task<MessagingTestHost> StartHostAsync(AttemptRecorder attempts)
    {
        var connectionString = await fixture.CreateDatabaseAsync();

        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(o => o.UseTestNpgsql(connectionString));

        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(ef =>
        {
            ef.UseTransaction();
            ef.UsePostgresInbox();
            ef.AddPostgresSagas();
        });
        builder.AddResilience(p => p.On<SagaInstanceNotFoundException>()
            .Retry(1, TimeSpan.FromMilliseconds(1), RetryBackoffType.Constant));
        builder.AddSaga<OrderSaga>();
        // outside the transaction, so an attempt is recorded after it committed or rolled back
        builder.ConfigureMessageBus(h =>
            h.UseConsume(attempts.CreateMiddleware(), before: "EntityFrameworkTransaction"));
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        }

        return await MessagingTestHost.StartAsync(provider, TestContext.Current.CancellationToken);
    }

    private sealed record StoreRows(int InboxClaims, int SagaStates);

    public sealed class OrderState : SagaStateBase;

    public sealed record OrderPlaced(Guid OrderId);

    public sealed record OrderConfirmed(Guid OrderId) : ICorrelatable
    {
        public Guid? CorrelationId => OrderId;
    }

    public sealed class OrderSaga : Saga<OrderState>
    {
        protected override void Configure(ISagaDescriptor<OrderState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<OrderPlaced>()
                .StateFactory(e => new OrderState { Id = e.OrderId })
                .TransitionTo("AwaitingConfirmation");

            descriptor.During("AwaitingConfirmation").OnEvent<OrderConfirmed>().TransitionTo("Completed");

            descriptor.Finally("Completed");
        }
    }

    /// <summary>
    /// Records the outcome of every consumer attempt and holds the first failed attempt until released.
    /// </summary>
    private sealed class AttemptRecorder
    {
        private readonly SemaphoreSlim _recorded = new(0);
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _failures;

        public ConcurrentQueue<string> Attempts { get; } = new();

        public void ReleaseFailedAttempt() => _release.TrySetResult();

        public async Task WaitForAttemptAsync()
            => Assert.True(
                await _recorded.WaitAsync(s_timeout, TestContext.Current.CancellationToken),
                "No consumer attempt was recorded within the timeout.");

        public ConsumerMiddlewareConfiguration CreateMiddleware()
            => new(
                (_, next) => async context =>
                {
                    try
                    {
                        await next(context);
                    }
                    catch (Exception ex)
                    {
                        Record(context, ex.GetType().Name);

                        if (Interlocked.Increment(ref _failures) == 1)
                        {
                            await _release.Task.WaitAsync(s_timeout);
                        }

                        throw;
                    }

                    Record(context, "Consumed");
                },
                "RecordAttempt");

        private void Record(IConsumeContext context, string outcome)
        {
            Attempts.Enqueue($"{context.GetMessage()!.GetType().Name}: {outcome}");
            _recorded.Release();
        }
    }
}
