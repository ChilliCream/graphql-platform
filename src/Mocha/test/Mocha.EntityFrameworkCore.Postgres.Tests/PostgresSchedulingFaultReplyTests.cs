using System.Buffers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Middlewares;
using Mocha.Scheduling;
using Mocha.Transport.InMemory;
using Npgsql;
using NpgsqlTypes;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresSchedulingFaultReplyTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(15);

    [Fact]
    public async Task Scheduler_Should_DeliverFaultReply_When_ReplyAddressIsNotADispatchEndpoint()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var host = await StartHostAsync(recorder);

        // The fault reply is addressed to the handler's queue under a foreign host name. The host
        // owns no dispatch endpoint for that address, only its reply dispatch endpoint can deliver there.
        var runtime = (MessagingRuntime)host.Services.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var queueAddress = transport.ReceiveEndpoints.Single(e => e.Kind == ReceiveEndpointKind.Default).Source.Address;
        var callerAddress = new UriBuilder(queueAddress) { Host = "caller" }.Uri;

        var headers = new Headers();
        headers.Set(MessageHeaders.MessageKind, MessageKind.Fault);

        var envelope = new MessageEnvelope
        {
            MessageId = Guid.NewGuid().ToString(),
            MessageType = runtime.GetMessageType(typeof(TestEvent)).Identity,
            DestinationAddress = callerAddress.ToString(),
            ContentType = "application/json",
            Headers = headers,
            Body = JsonSerializer.SerializeToUtf8Bytes(new TestEvent { Payload = "fault" })
        };

        // act
        await ScheduleAsync(host.Services, envelope);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler should have received the fault reply");
        var received = Assert.Single(recorder.Messages);
        Assert.Equal("fault", received.Payload);
    }

    private async Task<MessagingTestHost> StartHostAsync(MessageRecorder recorder)
    {
        var connectionString = await fixture.CreateDatabaseAsync();

        var services = new ServiceCollection();
        services.AddSingleton(recorder);
        services.AddDbContext<TestDbContext>(o => o.UseTestNpgsql(connectionString));

        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresScheduling());
        builder.AddEventHandler<TestEventHandler>();

        // The in-memory transport without its own scheduling store, so the EF Core Postgres
        // fallback dispatches the scheduled message.
        var transport = new InMemoryMessagingTransport(static _ => { });
        builder.ConfigureMessageBus(b => b.AddTransport(transport));

        var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        }

        return await MessagingTestHost.StartAsync(provider, TestContext.Current.CancellationToken);
    }

    // Replies cannot be scheduled through the bus, so the row is written the way the store writes it.
    private static async Task ScheduleAsync(IServiceProvider services, MessageEnvelope envelope)
    {
        var options = services
            .GetRequiredService<IOptionsMonitor<PostgresScheduledMessageOptions>>()
            .Get(typeof(TestDbContext).FullName);
        var scheduledTime = DateTimeOffset.UtcNow;

        using var scope = services.CreateScope();
        await using var connection = options.CreateConnection(scope.ServiceProvider);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = options.Queries.InsertMessage;
        command.Parameters.AddWithValue("@id", Guid.NewGuid());
        command.Parameters.Add(new NpgsqlParameter("@envelope", NpgsqlDbType.Json) { Value = WriteEnvelope(envelope) });
        command.Parameters.AddWithValue("@scheduled_time", scheduledTime.UtcDateTime);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

        services.GetRequiredService<ISchedulerSignal>().Notify(scheduledTime);
    }

    private static ReadOnlyMemory<byte> WriteEnvelope(MessageEnvelope envelope)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        new MessageEnvelopeWriter(writer).WriteMessage(envelope);
        writer.Flush();
        return buffer.WrittenMemory;
    }

    public sealed class TestEvent
    {
        public required string Payload { get; init; }
    }

    public sealed class TestEventHandler(MessageRecorder recorder) : IEventHandler<TestEvent>
    {
        public ValueTask HandleAsync(TestEvent message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    public sealed class MessageRecorder
    {
        private readonly SemaphoreSlim _semaphore = new(0);

        public List<TestEvent> Messages { get; } = [];

        public void Record(TestEvent message)
        {
            lock (Messages)
            {
                Messages.Add(message);
            }

            _semaphore.Release();
        }

        public Task<bool> WaitAsync(TimeSpan timeout) => _semaphore.WaitAsync(timeout);
    }
}
