using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Middlewares;
using Mocha.Outbox;
using Mocha.Transport.InMemory;
using Npgsql;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresOutboxDeliveryTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private static readonly DateTimeOffset s_now = new(2050, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProcessAsync_Should_RestorePreparedMessage_When_DeliveredOrRetried(bool retry)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var recorder = new DeliveryRecorder { FailFirstDelivery = retry };
        await using var provider = await CreateProviderAsync(connectionString, recorder);
        MessageEnvelope stored;
        int preparationAtInvocation;
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await db.Outbox.PublishAsync(new PreparedEvent { Value = "before-next" }, TestToken);
            preparationAtInvocation = recorder.PreparationCount;
            await db.SaveChangesAsync(TestToken);
            var json = await db.Set<OutboxMessage>().Select(x => x.Envelope).SingleAsync(TestToken);
            stored = MessageEnvelopeReader.Parse(Encoding.UTF8.GetBytes(json));
        }

        // act
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        var delivered = await recorder.Delivered.Task.WaitAsync(s_timeout, TestToken);
        await WaitForEmptyOutboxAsync(connectionString);

        // assert
        var expected = new DeliveryState(
            stored.MessageId, stored.CorrelationId, stored.ConversationId, stored.CausationId,
            stored.SourceAddress, stored.DestinationAddress, stored.ResponseAddress, stored.FaultAddress,
            stored.ContentType, stored.SentAt!.Value, stored.DeliverBy, "stored",
            provider.GetRequiredService<IMessagingRuntime>().Host.InstanceId,
            Encoding.UTF8.GetString(stored.Body.Span));
        Assert.Equal(Enumerable.Repeat(expected, retry ? 2 : 1), recorder.Attempts);
        Assert.Equal((1, 1, 1), (preparationAtInvocation, recorder.PreparationCount, recorder.DispatchCount));
        Assert.Equal("after-next", delivered.Value);
        Assert.Equal("after-next", JsonSerializer.Deserialize<PreparedEvent>(stored.Body.Span)!.Value);
    }

    [Fact]
    public async Task ProcessAsync_Should_DeliverLegacyMessage_When_OptionalAddressesAreMalformed()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var recorder = new DeliveryRecorder();
        await using var provider = await CreateProviderAsync(connectionString, recorder);
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var type = runtime.GetMessageType(typeof(PreparedEvent));
        var endpoint = runtime.GetPublishEndpoint(type);
        await MessageRows.InsertOutboxAsync(connectionString, new MessageEnvelope
        {
            MessageId = "legacy-message",
            MessageType = type.Identity,
            ContentType = MessageContentType.Json.Value,
            DestinationAddress = endpoint.Address.ToString(),
            SourceAddress = string.Empty,
            ResponseAddress = "relative-response",
            FaultAddress = "relative-fault",
            Body = JsonSerializer.SerializeToUtf8Bytes(new PreparedEvent { Value = "legacy" })
        }, TestToken);

        // act
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        var delivered = await recorder.Delivered.Task.WaitAsync(s_timeout, TestToken);
        await WaitForEmptyOutboxAsync(connectionString);

        // assert
        var attempt = Assert.Single(recorder.Attempts);
        Assert.Equal<string?>(
            [null, null, null],
            [attempt.SourceAddress, attempt.ResponseAddress, attempt.FaultAddress]);
        Assert.Equal("legacy", delivered.Value);
        Assert.Equal((0, 0), (recorder.PreparationCount, recorder.DispatchCount));
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_ReportPreparationFailure_When_SerializationFails()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var recorder = new DeliveryRecorder { FailSerialization = true };
        await using var provider = await CreateProviderAsync(connectionString, recorder);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await db.Outbox.PublishAsync(new PreparedEvent { Value = "before-next" }, TestToken));
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal([exception], recorder.DispatchErrors);
        Assert.Empty(db.ChangeTracker.Entries<OutboxMessage>());
        Assert.Equal(0, await db.Set<OutboxMessage>().CountAsync(TestToken));
        Assert.Empty(recorder.Attempts);
    }

    private static async Task<ServiceProvider> CreateProviderAsync(
        string connectionString,
        DeliveryRecorder recorder)
    {
        var services = new ServiceCollection();
        services.AddSingleton(recorder);
        services.AddSingleton<IMessagingDiagnosticEventListener>(recorder);
        services.AddSingleton<IOutboxSignal, StubOutboxSignal>();
        services.AddDbContext<TestDbContext>(options => options.UseTestNpgsql(connectionString));
        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(options => options.UsePostgresOutbox());
        builder.AddEventHandler<PreparedEventHandler>();
        builder.AddInMemory();
        builder.ConfigureMessageBus(bus => bus.UseDispatch(new DispatchMiddlewareConfiguration(
            (_, next) => async context =>
            {
                recorder.PreparationCount++;
                await next(context);
                ((PreparedEvent)context.Message!).Value = "after-next";
                context.MessageId = "stable-message";
                context.CorrelationId = "stable-correlation";
                context.ConversationId = "stable-conversation";
                context.CausationId = "stable-causation";
                context.SourceAddress = new Uri("memory://localhost/q/original");
                context.ResponseAddress = new Uri("memory://localhost/q/response");
                context.FaultAddress = new Uri("memory://localhost/q/fault");
                context.Headers.Set("prepared-header", "stored");
                context.SentAt = s_now.AddMinutes(-5);
                context.DeliverBy = s_now.AddHours(1);
                if (recorder.FailSerialization)
                {
                    context.ContentType = new MessageContentType("application/unsupported");
                }
            },
            "PrepareOriginalMessage"), before: "Serialization"));

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(TestToken);
        var endpoint = runtime.GetPublishEndpoint(runtime.GetMessageType(typeof(PreparedEvent)));
        var recordingEndpoint = new RecordingDispatchEndpoint(endpoint, recorder);
        recordingEndpoint.Initialize(runtime, new DispatchEndpointConfiguration { Name = endpoint.Name });
        recordingEndpoint.Complete(runtime);
        runtime.Endpoints.Remove(endpoint);
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync(TestToken);
        return provider;
    }

    private static async Task WaitForEmptyOutboxAsync(string connectionString)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        cancellation.CancelAfter(s_timeout);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellation.Token);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM outbox_messages";
        while ((long)(await command.ExecuteScalarAsync(cancellation.Token))! != 0)
        {
            await Task.Delay(50, cancellation.Token);
        }
    }

    public sealed class PreparedEvent
    {
        public string Value { get; set; } = string.Empty;
    }

    public sealed class PreparedEventHandler(DeliveryRecorder recorder) : IEventHandler<PreparedEvent>
    {
        public ValueTask HandleAsync(PreparedEvent message, CancellationToken cancellationToken)
        {
            recorder.Delivered.TrySetResult(message);
            return default;
        }
    }

    public sealed class DeliveryRecorder : MessagingDiagnosticEventListener
    {
        internal ConcurrentQueue<DeliveryState> Attempts { get; } = new();

        public TaskCompletionSource<PreparedEvent> Delivered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int PreparationCount { get; set; }

        public int DispatchCount { get; private set; }

        public bool FailFirstDelivery { get; init; }

        public bool FailSerialization { get; init; }

        public ConcurrentQueue<Exception> DispatchErrors { get; } = new();

        public override void DispatchError(IDispatchContext context, Exception exception)
            => DispatchErrors.Enqueue(exception);

        public override IDisposable Dispatch(IDispatchContext context)
        {
            DispatchCount++;
            return EmptyScope;
        }

        internal void RecordAttempt(IDispatchContext context)
        {
            var envelope = context.Envelope!;

            Attempts.Enqueue(new DeliveryState(
                context.MessageId, context.CorrelationId, context.ConversationId, context.CausationId,
                context.SourceAddress?.ToString(), context.DestinationAddress?.ToString(),
                context.ResponseAddress?.ToString(), context.FaultAddress?.ToString(),
                context.ContentType?.Value, context.SentAt, context.DeliverBy,
                (string?)context.Headers.GetValue("prepared-header"), context.Host.InstanceId,
                Encoding.UTF8.GetString(envelope.Body.Span)));

            if (FailFirstDelivery && Attempts.Count == 1)
            {
                throw new InvalidOperationException("The first delivery failed deliberately.");
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
            recorder.RecordAttempt(context);
            return endpoint.DispatchInternalAsync(context);
        }
    }

    internal sealed record DeliveryState(
        string? MessageId,
        string? CorrelationId,
        string? ConversationId,
        string? CausationId,
        string? SourceAddress,
        string? DestinationAddress,
        string? ResponseAddress,
        string? FaultAddress,
        string? ContentType,
        DateTimeOffset? SentAt,
        DateTimeOffset? DeliverBy,
        string? Header,
        Guid HostInstanceId,
        string Body);
}
