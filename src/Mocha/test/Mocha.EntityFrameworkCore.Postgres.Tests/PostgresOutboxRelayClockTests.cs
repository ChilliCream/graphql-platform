using System.Data;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Middlewares;
using Mocha.Outbox;
using Mocha.Transport.InMemory;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresOutboxRelayClockTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private static readonly DateTimeOffset s_now = new(2050, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ProcessAsync_Should_RetryFailedSend_When_RegisteredClockDoesNotAdvance()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var recorder = new DeliveryRecorder { FailFirstAttempt = true };
        // with an hour-long polling interval, only the wait until the failed row is due can retry it in time
        await using var provider = await CreateProviderAsync(connectionString, TimeSpan.FromHours(1), recorder);
        await MessageRows.InsertOutboxAsync(connectionString, CreateEnvelope(provider), TestToken);

        // act
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        var delivered = await recorder.Delivered.Task.WaitAsync(s_timeout, TestToken);

        // assert
        Assert.Equal(2, recorder.Attempts);
        Assert.Equal("pending", delivered.Value);
    }

    [Fact]
    public async Task ProcessAsync_Should_DeliverUnsignaledMessage_When_RegisteredClockDoesNotAdvance()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var recorder = new DeliveryRecorder();
        await using var provider = await CreateProviderAsync(
            connectionString, TimeSpan.FromMilliseconds(100), recorder);
        var firstPollEnded = ObserveFirstPoll(provider);
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        // the first poll ended with an empty outbox, so only a later poll can find the next row
        await firstPollEnded.WaitAsync(s_timeout, TestToken);

        // act
        await MessageRows.InsertOutboxAsync(connectionString, CreateEnvelope(provider), TestToken);
        var delivered = await recorder.Delivered.Task.WaitAsync(s_timeout, TestToken);

        // assert
        Assert.Equal(1, recorder.Attempts);
        Assert.Equal("pending", delivered.Value);
    }

    private static Task ObserveFirstPoll(ServiceProvider provider)
    {
        var firstPollEnded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var options = provider.GetRequiredService<IOptionsMonitor<PostgresMessageOutboxOptions>>()
            .Get(typeof(TestDbContext).FullName);
        var createConnection = options.CreateConnection;
        options.CreateConnection = services =>
        {
            var connection = createConnection(services);
            connection.StateChange += (_, args) =>
            {
                if (args.CurrentState == ConnectionState.Closed)
                {
                    firstPollEnded.TrySetResult();
                }
            };
            return connection;
        };

        return firstPollEnded.Task;
    }

    private static MessageEnvelope CreateEnvelope(IServiceProvider provider)
    {
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var type = runtime.GetMessageType(typeof(ClockEvent));
        return new MessageEnvelope
        {
            MessageId = Guid.NewGuid().ToString(),
            MessageType = type.Identity,
            ContentType = MessageContentType.Json.Value,
            DestinationAddress = runtime.GetPublishEndpoint(type).Address.ToString(),
            Body = JsonSerializer.SerializeToUtf8Bytes(new ClockEvent { Value = "pending" })
        };
    }

    private static async Task<ServiceProvider> CreateProviderAsync(
        string connectionString,
        TimeSpan pollingInterval,
        DeliveryRecorder recorder)
    {
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(s_now));
        services.AddKeyedSingleton<IOutboxSignal, StubOutboxSignal>(typeof(TestDbContext));
        services.AddSingleton(recorder);
        services.AddDbContext<TestDbContext>(options => options.UseTestNpgsql(connectionString));
        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(
            ef => ef.UsePostgresOutbox(options => options.PollingInterval = pollingInterval));
        builder.AddEventHandler<ClockEventHandler>();
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(TestToken);
        var endpoint = runtime.GetPublishEndpoint(runtime.GetMessageType(typeof(ClockEvent)));
        var recordingEndpoint = new RecordingDispatchEndpoint(endpoint, recorder);
        recordingEndpoint.Initialize(runtime, new DispatchEndpointConfiguration { Name = endpoint.Name });
        recordingEndpoint.Complete(runtime);
        runtime.Endpoints.Remove(endpoint);
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync(TestToken);
        return provider;
    }

    public sealed class ClockEvent
    {
        public required string Value { get; init; }
    }

    public sealed class ClockEventHandler(DeliveryRecorder recorder) : IEventHandler<ClockEvent>
    {
        public ValueTask HandleAsync(ClockEvent message, CancellationToken cancellationToken)
        {
            recorder.Delivered.TrySetResult(message);
            return default;
        }
    }

    public sealed class DeliveryRecorder
    {
        private int _attempts;

        public bool FailFirstAttempt { get; init; }

        public int Attempts => Volatile.Read(ref _attempts);

        public TaskCompletionSource<ClockEvent> Delivered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void RecordAttempt()
        {
            if (Interlocked.Increment(ref _attempts) == 1 && FailFirstAttempt)
            {
                throw new InvalidOperationException("The first send failed deliberately.");
            }
        }
    }

    private sealed class RecordingDispatchEndpoint(DispatchEndpoint endpoint, DeliveryRecorder recorder)
        : DispatchEndpoint(endpoint.Transport)
    {
        protected override void OnInitialize(
            IMessagingConfigurationContext context,
            DispatchEndpointConfiguration configuration)
        {
            Address = endpoint.Address;
            Destination = endpoint.Destination;
        }

        protected override void OnComplete(
            IMessagingConfigurationContext context,
            DispatchEndpointConfiguration configuration)
        { }

        protected override ValueTask DispatchAsync(IDispatchContext context)
        {
            recorder.RecordAttempt();
            return endpoint.DispatchInternalAsync(context);
        }
    }
}
