using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Middlewares;
using Mocha.Outbox;
using Mocha.Transport.InMemory;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresOutboxExpirationTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private static readonly DateTimeOffset s_now = new(2050, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    public async Task ProcessAsync_Should_DiscardExpiredMessage_When_DeadlineHasBeenReached(
        int seconds,
        bool unresolvable)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var recorder = new DeliveryRecorder();
        await using var provider = await CreateProviderAsync(
            connectionString, new FakeTimeProvider(s_now), recorder);
        var envelope = CreateEnvelope(provider, s_now.AddSeconds(seconds), unresolvable);
        var id = await MessageRows.InsertOutboxAsync(connectionString, envelope, TestToken);

        // act
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        var remainingAttempts = await MessageRows.WaitForOutboxRetryAsync(
            connectionString, id, s_timeout, TestToken);

        // assert
        Assert.Null(remainingAttempts);
        Assert.Equal(0, recorder.Attempts);
        Assert.False(recorder.Delivered.Task.IsCompleted);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProcessAsync_Should_DeliverMessage_When_DeadlineHasNotBeenReached(bool hasDeadline)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var recorder = new DeliveryRecorder();
        await using var provider = await CreateProviderAsync(
            connectionString, new FakeTimeProvider(s_now), recorder);
        var envelope = CreateEnvelope(provider, hasDeadline ? s_now.AddHours(1) : null, unresolvable: false);
        var id = await MessageRows.InsertOutboxAsync(connectionString, envelope, TestToken);

        // act
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        var delivered = await recorder.Delivered.Task.WaitAsync(s_timeout, TestToken);
        var remainingAttempts = await MessageRows.WaitForOutboxRetryAsync(
            connectionString, id, s_timeout, TestToken);

        // assert
        Assert.Null(remainingAttempts);
        Assert.Equal(1, recorder.Attempts);
        Assert.Equal("pending", delivered.Value);
    }

    [Fact]
    public async Task ProcessAsync_Should_DiscardExpiredMessage_When_DeadlinePassesAfterFailedDelivery()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var clock = new FakeTimeProvider(s_now);
        var recorder = new DeliveryRecorder
        {
            OnFirstAttempt = () =>
            {
                clock.Advance(TimeSpan.FromHours(2));
                throw new InvalidOperationException("The first delivery failed deliberately.");
            }
        };
        await using var provider = await CreateProviderAsync(connectionString, clock, recorder);
        var id = await MessageRows.InsertOutboxAsync(
            connectionString, CreateEnvelope(provider, s_now.AddHours(1), unresolvable: false), TestToken);

        // act
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        var remainingAttempts = await MessageRows.WaitForOutboxRetryAsync(
            connectionString, id, s_timeout, TestToken);

        // assert
        Assert.Null(remainingAttempts);
        Assert.Equal(1, recorder.Attempts);
        Assert.False(recorder.Delivered.Task.IsCompleted);
    }

    private static MessageEnvelope CreateEnvelope(
        IServiceProvider provider,
        DateTimeOffset? deadline,
        bool unresolvable)
    {
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var type = runtime.GetMessageType(typeof(PendingEvent));
        return new MessageEnvelope
        {
            MessageId = Guid.NewGuid().ToString(),
            MessageType = unresolvable ? "urn:message:unknown" : type.Identity,
            ContentType = MessageContentType.Json.Value,
            DestinationAddress = unresolvable ? "unknown://destination" : runtime.GetPublishEndpoint(type).Address.ToString(),
            DeliverBy = deadline,
            Body = JsonSerializer.SerializeToUtf8Bytes(new PendingEvent { Value = "pending" })
        };
    }

    private static async Task<ServiceProvider> CreateProviderAsync(
        string connectionString,
        TimeProvider clock,
        DeliveryRecorder recorder)
    {
        var services = new ServiceCollection();
        services.AddSingleton(clock);
        services.AddSingleton(recorder);
        services.AddSingleton<IMessagingDiagnosticEventListener>(recorder);
        services.AddDbContext<TestDbContext>(options => options.UseTestNpgsql(connectionString));
        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(options => options.UsePostgresOutbox());
        builder.AddEventHandler<PendingEventHandler>();
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        await ((MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>()).StartAsync(TestToken);
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync(TestToken);
        return provider;
    }

    public sealed class PendingEvent
    {
        public required string Value { get; init; }
    }

    public sealed class PendingEventHandler(DeliveryRecorder recorder) : IEventHandler<PendingEvent>
    {
        public ValueTask HandleAsync(PendingEvent message, CancellationToken cancellationToken)
        {
            recorder.Delivered.TrySetResult(message);
            return default;
        }
    }

    public sealed class DeliveryRecorder : MessagingDiagnosticEventListener
    {
        private int _attempts;

        public int Attempts => Volatile.Read(ref _attempts);

        public Action? OnFirstAttempt { get; init; }

        public TaskCompletionSource<PendingEvent> Delivered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override IDisposable Dispatch(IDispatchContext context)
        {
            if (Interlocked.Increment(ref _attempts) == 1)
            {
                OnFirstAttempt?.Invoke();
            }

            return EmptyScope;
        }
    }
}
