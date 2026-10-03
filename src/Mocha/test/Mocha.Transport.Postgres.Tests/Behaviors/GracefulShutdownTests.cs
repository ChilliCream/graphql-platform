using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;
using Npgsql;

namespace Mocha.Transport.Postgres.Tests.Behaviors;

[Collection("Postgres")]
public sealed class GracefulShutdownTests(PostgresFixture fixture)
{
    private const string QueueName = "shutdown-orders";
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task StopAsync_Should_UnregisterConsumerAndHandOverQueue_When_HostStops()
    {
        // arrange
        await using var db = await fixture.CreateDatabaseAsync();
        var firstRecorder = new MessageRecorder();
        await using var first = await StartBusAsync<OrderCreatedHandler>(db, firstRecorder);
        await first.PublishAsync(new OrderCreated { OrderId = "ORD-BEFORE-STOP" });
        Assert.True(await firstRecorder.WaitAsync(s_timeout), "The first bus should consume before it stops");
        var beforeStop = await InspectAsync(db);

        // act
        await first.StopAsync(TestContext.Current.CancellationToken);

        // assert
        var afterStop = await InspectAsync(db);
        var secondRecorder = new MessageRecorder();
        await using var second = await StartBusAsync<OrderCreatedHandler>(db, secondRecorder);
        for (var i = 1; i <= 5; i++)
        {
            await second.PublishAsync(new OrderCreated { OrderId = $"ORD-{i}" });
        }

        var secondReceivedAll = await secondRecorder.WaitAsync(s_timeout, expectedCount: 5);

        new
        {
            BeforeStop = beforeStop,
            AfterStop = afterStop,
            FirstBusReceived = firstRecorder.Messages.Count,
            SecondBusReceivedAll = secondReceivedAll
        }.MatchInlineSnapshot(
            """
            {
              "BeforeStop": {
                "Consumers": 1,
                "TemporaryQueues": 1,
                "Queues": []
              },
              "AfterStop": {
                "Consumers": 0,
                "TemporaryQueues": 0,
                "Queues": []
              },
              "FirstBusReceived": 1,
              "SecondBusReceivedAll": true
            }
            """);
    }

    [Fact]
    public async Task StopAsync_Should_ReturnUnstartedAndReleaseCancelledMessages_When_StopIsCancelled()
    {
        // arrange
        await using var db = await fixture.CreateDatabaseAsync();
        var gate = new HandlerGate();
        await using var first = await StartBusAsync<GatedOrderHandler>(db, new MessageRecorder(), gate);

        // the messages become due together, so a single batch leases all of them
        var scheduledTime = TimeProvider.System.GetUtcNow().AddSeconds(2);
        for (var i = 1; i <= 3; i++)
        {
            await first.PublishAsync(new OrderCreated { OrderId = $"ORD-{i}" }, scheduledTime);
        }

        await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        var beforeStop = await InspectAsync(db);
        using var shutdownTimeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        // act
        await first.StopAsync(shutdownTimeout.Token);

        // assert
        var afterStop = await InspectAsync(db);
        var secondRecorder = new MessageRecorder();
        await using var second = await StartBusAsync<OrderCreatedHandler>(db, secondRecorder);
        var secondReceivedAll = await secondRecorder.WaitAsync(TimeSpan.FromSeconds(10), expectedCount: 3);

        new
        {
            HandlerCancelled = gate.Cancelled.Task.IsCompleted,
            BeforeStop = beforeStop,
            AfterStop = afterStop,
            SecondBusReceivedAll = secondReceivedAll
        }.MatchInlineSnapshot(
            """
            {
              "HandlerCancelled": true,
              "BeforeStop": {
                "Consumers": 1,
                "TemporaryQueues": 1,
                "Queues": [
                  {
                    "Queue": "shutdown-orders",
                    "Messages": 3,
                    "Leased": 3,
                    "DeliveryCount": 3,
                    "LastDelivered": 3
                  }
                ]
              },
              "AfterStop": {
                "Consumers": 0,
                "TemporaryQueues": 0,
                "Queues": [
                  {
                    "Queue": "shutdown-orders",
                    "Messages": 3,
                    "Leased": 0,
                    "DeliveryCount": 1,
                    "LastDelivered": 1
                  }
                ]
              },
              "SecondBusReceivedAll": true
            }
            """);
    }

    [Fact]
    public async Task StopAsync_Should_CompleteInFlightMessage_When_HandlerFinishesBeforeStopIsCancelled()
    {
        // arrange
        await using var db = await fixture.CreateDatabaseAsync();
        var gate = new HandlerGate();
        var recorder = new MessageRecorder();
        await using var bus = await StartBusAsync<GatedOrderHandler>(db, recorder, gate);
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-IN-FLIGHT" });
        await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // act
        var stop = bus.StopAsync(TestContext.Current.CancellationToken);
        await Task.Delay(500, TestContext.Current.CancellationToken);
        var stoppedBeforeHandlerFinished = stop.IsCompleted;
        gate.Release.TrySetResult();
        await stop.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        new
        {
            StoppedBeforeHandlerFinished = stoppedBeforeHandlerFinished,
            HandlerCompleted = recorder.Messages.Count,
            AfterStop = await InspectAsync(db)
        }.MatchInlineSnapshot(
            """
            {
              "StoppedBeforeHandlerFinished": false,
              "HandlerCompleted": 1,
              "AfterStop": {
                "Consumers": 0,
                "TemporaryQueues": 0,
                "Queues": []
              }
            }
            """);
    }

    [Fact]
    public async Task DisposeAsync_Should_UnregisterConsumerAndCloseConnections_When_ProviderDisposedWithoutStop()
    {
        // arrange
        await using var db = await fixture.CreateDatabaseAsync();
        var recorder = new MessageRecorder();
        var bus = await StartBusAsync<OrderCreatedHandler>(db, recorder);
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" });
        Assert.True(await recorder.WaitAsync(s_timeout), "The bus should consume before it is disposed");
        var connectionsWhileRunning = await CountConnectionsAsync(db);

        // act
        await bus.DisposeAsync();

        // assert
        var connectionsAfterDispose = await WaitForNoConnectionsAsync(db);
        new
        {
            HadConnectionsWhileRunning = connectionsWhileRunning > 0,
            ConnectionsAfterDispose = connectionsAfterDispose,
            AfterDispose = await InspectAsync(db)
        }.MatchInlineSnapshot(
            """
            {
              "HadConnectionsWhileRunning": true,
              "ConnectionsAfterDispose": 0,
              "AfterDispose": {
                "Consumers": 0,
                "TemporaryQueues": 0,
                "Queues": []
              }
            }
            """);
    }

    [Fact]
    public async Task DisposeAsync_Should_Complete_When_StartFailed()
    {
        // arrange
        var connectionString = new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
        {
            Database = "mocha_shutdown_missing_database"
        }.ConnectionString;
        var bus = new HostedTestBus(
            new ServiceCollection()
                .AddSingleton(new MessageRecorder())
                .AddMessageBus()
                .AddEventHandler<OrderCreatedHandler>()
                .AddPostgres(t => t.ConnectionString(connectionString))
                .Services.BuildServiceProvider());
        var runtime = bus.Runtime;
        var startFailure = await Assert.ThrowsAsync<PostgresException>(
            () => bus.StartAsync(TestContext.Current.CancellationToken));

        // act
        await bus.DisposeAsync();

        // assert
        Assert.Equal(PostgresErrorCodes.InvalidCatalogName, startFailure.SqlState);
        Assert.All(runtime.Transports, t => Assert.False(t.IsStarted));
    }

    private static async Task<HostedTestBus> StartBusAsync<THandler>(
        DatabaseContext db,
        MessageRecorder recorder,
        HandlerGate? gate = null)
        where THandler : class, IEventHandler<OrderCreated>
    {
        var services = new ServiceCollection().AddSingleton(recorder).AddSingleton(gate ?? new HandlerGate());

        return await services
            .AddMessageBus()
            .AddEventHandler<THandler>()
            .AddPostgres(t =>
            {
                t.ConnectionString(db.ConnectionString);
                t.Endpoint(QueueName).Handler<THandler>().MaxConcurrency(1);
            })
            .StartHostedTestBusAsync();
    }

    private static async Task<DatabaseState> InspectAsync(DatabaseContext db)
    {
        var schema = new PostgresSchemaOptions();
        await using var connection = new NpgsqlConnection(WithoutPooling(db.ConnectionString));
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
            SELECT
                (SELECT count(*) FROM {schema.ConsumersTable}),
                (SELECT count(*) FROM {schema.QueueTable} WHERE consumer_id IS NOT NULL);

            SELECT q.name, count(*), count(m.consumer_id), sum(m.delivery_count), count(m.last_delivered)
            FROM {schema.MessageTable} m
            INNER JOIN {schema.QueueTable} q ON q.id = m.queue_id
            GROUP BY q.name
            ORDER BY q.name;
            """;

        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        await reader.ReadAsync(TestContext.Current.CancellationToken);
        var consumers = reader.GetInt64(0);
        var temporaryQueues = reader.GetInt64(1);

        await reader.NextResultAsync(TestContext.Current.CancellationToken);
        var queues = new List<QueueState>();
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            queues.Add(
                new QueueState(
                    reader.GetString(0),
                    reader.GetInt64(1),
                    reader.GetInt64(2),
                    reader.GetInt64(3),
                    reader.GetInt64(4)));
        }

        return new DatabaseState(consumers, temporaryQueues, queues);
    }

    private async Task<long> CountConnectionsAsync(DatabaseContext db)
    {
        await using var connection = new NpgsqlConnection(WithoutPooling(fixture.ConnectionString));
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM pg_stat_activity WHERE datname = @database";
        command.Parameters.AddWithValue("database", db.DatabaseName);
        return (long)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }

    private async Task<long> WaitForNoConnectionsAsync(DatabaseContext db)
    {
        // backends exit shortly after their client closes the connection
        var deadline = DateTime.UtcNow.AddSeconds(5);
        long count;
        while ((count = await CountConnectionsAsync(db)) > 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        return count;
    }

    private static string WithoutPooling(string connectionString)
        => new NpgsqlConnectionStringBuilder(connectionString) { Pooling = false }.ConnectionString;

    private sealed record DatabaseState(long Consumers, long TemporaryQueues, IReadOnlyList<QueueState> Queues);

    private sealed record QueueState(string Queue, long Messages, long Leased, long DeliveryCount, long LastDelivered);

    public sealed class HandlerGate
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class GatedOrderHandler(HandlerGate gate, MessageRecorder recorder) : IEventHandler<OrderCreated>
    {
        public async ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            gate.Started.TrySetResult();

            try
            {
                await gate.Release.Task.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                gate.Cancelled.TrySetResult();
                throw;
            }

            recorder.Record(message);
        }
    }
}
