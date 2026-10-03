using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;
using Npgsql;

namespace Mocha.Transport.Postgres.Tests.Behaviors;

[Collection("Postgres")]
public sealed class ShutdownRecoveryTests(PostgresFixture fixture)
{
    [Fact]
    public async Task StopAsync_Should_KeepReplyEndpoint_When_AnotherTransportHasAnActiveRequest()
    {
        // arrange
        await using var inputDb = await fixture.CreateDatabaseAsync("shutdown_cross_transport_input");
        await using var remoteDb = await fixture.CreateDatabaseAsync("shutdown_cross_transport_remote");
        var gate = new RequestGate();
        await using var remote = await new ServiceCollection()
            .AddSingleton(gate)
            .AddMessageBus()
            .AddRequestHandler<RemoteHandler>()
            .AddPostgres(t =>
            {
                t.Schema("remote");
                t.ConnectionString(remoteDb.ConnectionString);
                t.Endpoint("request").Handler<RemoteHandler>();
            })
            .StartHostedTestBusAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(gate)
            .AddMessageBus()
            .AddEventHandler<RequestingHandler>()
            .AddPostgres(t =>
            {
                t.Name("input").Schema("input").BindExplicitly();
                t.ConnectionString(inputDb.ConnectionString);
                t.Endpoint("inbound").Handler<RequestingHandler>();
                t.DispatchEndpoint("inbound").ToQueue("inbound").Publish<OrderCreated>();
            })
            .AddPostgres(t =>
            {
                t.Name("remote").Schema("remote").BindExplicitly();
                t.ConnectionString(remoteDb.ConnectionString);
                t.DispatchEndpoint("request").ToQueue("request").Send<GetOrderStatus>();
            })
            .StartHostedTestBusAsync();
        await bus.PublishAsync(new OrderCreated { OrderId = "review" });
        await gate.Started.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
        using var shutdownTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // act
        var stop = bus.StopAsync(shutdownTimeout.Token);
        await Task.Delay(500, TestContext.Current.CancellationToken);
        var replyRunning = bus.Runtime.Transports.Single(t => t.Name == "remote").ReplyReceiveEndpoint!.IsStarted;
        gate.Release.TrySetResult();
        await stop.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken);

        // assert
        new
        {
            ReplyEndpointRunningDuringDrain = replyRunning,
            ResponseReceived = gate.ResponseReceived.Task.IsCompleted,
            RuntimeStopped = !bus.Runtime.IsStarted
        }.MatchInlineSnapshot(
            """
            {
              "ReplyEndpointRunningDuringDrain": true,
              "ResponseReceived": true,
              "RuntimeStopped": true
            }
            """);
    }

    [Fact]
    public async Task StopAsync_Should_ReleaseLease_When_HandlerOutlastsGracePeriod()
    {
        // arrange
        await using var db = await fixture.CreateDatabaseAsync();
        var gate = new HandlerGate();
        await using var bus = await new ServiceCollection()
            .AddSingleton(gate)
            .AddMessageBus()
            .AddEventHandler<GatedHandler>()
            .AddPostgres(t =>
            {
                t.ConnectionString(db.ConnectionString);
                t.ModifyOptions(o => o.Shutdown.CancellationGracePeriod = TimeSpan.FromMilliseconds(500));
                t.Endpoint("shutdown-orders").Handler<GatedHandler>().MaxConcurrency(1);
            })
            .StartHostedTestBusAsync();
        await bus.PublishAsync(new OrderCreated { OrderId = "review" });
        await gate.Started.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

        // act
        await bus.StopAsync(new CancellationToken(canceled: true));
        await using var connection = new NpgsqlConnection(db.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT count(*), count(consumer_id), sum(delivery_count), count(last_delivered)
            FROM mocha_message;
            """;
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        await reader.ReadAsync(TestContext.Current.CancellationToken);
        var returned = new
        {
            Messages = reader.GetInt64(0),
            Leased = reader.GetInt64(1),
            DeliveryCount = reader.GetInt64(2),
            LastDelivered = reader.GetInt64(3)
        };
        var recorder = new MessageRecorder();
        await using var second = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddPostgres(t =>
            {
                t.ConnectionString(db.ConnectionString);
                t.Endpoint("shutdown-orders").Handler<OrderCreatedHandler>();
            })
            .StartHostedTestBusAsync();
        bool redelivered;
        try
        {
            redelivered = await recorder.WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally
        {
            gate.Release.TrySetResult();
        }

        await gate.Finished.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        new { Returned = returned, Redelivered = redelivered }.MatchInlineSnapshot(
            """
            {
              "Returned": {
                "Messages": 1,
                "Leased": 0,
                "DeliveryCount": 1,
                "LastDelivered": 1
              },
              "Redelivered": true
            }
            """);
    }

    public sealed class HandlerGate
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Finished { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class GatedHandler(HandlerGate gate) : IEventHandler<OrderCreated>
    {
        public async ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            gate.Started.TrySetResult();
            await gate.Release.Task;
            gate.Finished.TrySetResult();
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    public sealed class RequestGate
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ResponseReceived { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class RemoteHandler(RequestGate gate) : IEventRequestHandler<GetOrderStatus, OrderStatusResponse>
    {
        public async ValueTask<OrderStatusResponse> HandleAsync(GetOrderStatus message, CancellationToken cancellationToken)
        {
            gate.Started.TrySetResult();
            await gate.Release.Task.WaitAsync(cancellationToken);
            return new OrderStatusResponse { OrderId = message.OrderId, Status = "completed" };
        }
    }

    public sealed class RequestingHandler(IMessageBus messageBus, RequestGate gate) : IEventHandler<OrderCreated>
    {
        public async ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            await messageBus.RequestAsync(new GetOrderStatus { OrderId = message.OrderId }, cancellationToken);
            gate.ResponseReceived.TrySetResult();
        }
    }
}
