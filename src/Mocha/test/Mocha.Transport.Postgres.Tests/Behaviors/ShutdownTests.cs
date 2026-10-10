using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mocha.Transport.Postgres.Tests.Helpers;
using Npgsql;

namespace Mocha.Transport.Postgres.Tests.Behaviors;

[Collection("Postgres")]
public sealed class ShutdownTests(PostgresFixture fixture)
{
    [Fact]
    public async Task StopAsync_Should_UnregisterConsumer_When_HostStops()
    {
        // arrange
        await using var db = await fixture.CreateDatabaseAsync("shutdown_unregister");
        await using var provider = CreateProvider<NoOpHandler>(db.ConnectionString, new HandlerGate(), new LogRecorder());
        var services = provider.GetServices<IHostedService>().ToArray();
        await StartAsync(services);
        var consumersBeforeStop = await CountConsumersAsync(db.ConnectionString);

        // act
        await StopAsync(services);

        // assert
        Assert.Equal(1, consumersBeforeStop);
        Assert.Equal(0, await CountConsumersAsync(db.ConnectionString));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StopAsync_Should_ReleaseInterruptedMessages_When_HostStops(bool failAfterCancellation)
    {
        // arrange
        await using var db = await fixture.CreateDatabaseAsync($"shutdown_release_{failAfterCancellation}");
        var gate = new HandlerGate { FailAfterCancellation = failAfterCancellation };
        var logs = new LogRecorder();
        await using var provider = CreateProvider<BlockingHandler>(db.ConnectionString, gate, logs);
        var services = provider.GetServices<IHostedService>().ToArray();
        await StartAsync(services);
        await PublishAsync(provider, count: 2);
        await gate.Started.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // act
        await StopAsync(services);

        // assert
        Assert.True(gate.Cancelled);
        Assert.Equal([(false, null), (false, null)], await ReadMessagesAsync(db.ConnectionString));
        Assert.Empty(logs.Entries);
    }

    [Fact]
    public async Task StopAsync_Should_DeleteMessage_When_HandlerCompletesAfterStopIsRequested()
    {
        // arrange
        await using var db = await fixture.CreateDatabaseAsync("shutdown_complete");
        var gate = new HandlerGate();
        var logs = new LogRecorder();
        await using var provider = CreateProvider<CompletingHandler>(db.ConnectionString, gate, logs);
        var services = provider.GetServices<IHostedService>().ToArray();
        await StartAsync(services);
        await PublishAsync(provider, count: 1);
        await gate.Started.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // act
        await StopAsync(services);

        // assert
        Assert.Empty(await ReadMessagesAsync(db.ConnectionString));
        Assert.Empty(logs.Entries);
    }

    private static ServiceProvider CreateProvider<THandler>(
        string connectionString,
        HandlerGate gate,
        LogRecorder logs)
        where THandler : class, IEventHandler<TestEvent>
        => new ServiceCollection()
            .AddSingleton(gate)
            .AddLogging(b => b.AddProvider(logs))
            .AddMessageBus()
            .AddEventHandler<THandler>()
            .AddPostgres(t =>
            {
                t.ConnectionString(connectionString);
                t.Endpoint("shutdown").Handler<THandler>().MaxConcurrency(1);
            })
            .Services.BuildServiceProvider();

    private static async Task StartAsync(IHostedService[] services)
    {
        foreach (var service in services)
        {
            await service.StartAsync(TestContext.Current.CancellationToken);
        }
    }

    private static async Task StopAsync(IHostedService[] services)
    {
        foreach (var service in services.Reverse())
        {
            await service.StopAsync(CancellationToken.None)
                .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        }
    }

    private static async Task PublishAsync(IServiceProvider provider, int count)
    {
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        for (var i = 0; i < count; i++)
        {
            await bus.PublishAsync(new TestEvent(), TestContext.Current.CancellationToken);
        }
    }

    private static async Task<long> CountConsumersAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM mocha_consumers";
        return (long)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }

    private static async Task<List<(bool Leased, string? ErrorReason)>> ReadMessagesAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT consumer_id IS NOT NULL, error_reason FROM mocha_message";
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        var messages = new List<(bool Leased, string? ErrorReason)>();
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            messages.Add((reader.GetBoolean(0), reader.IsDBNull(1) ? null : reader.GetString(1)));
        }

        return messages;
    }

    public sealed class TestEvent;

    public sealed class HandlerGate
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Cancelled { get; set; }
        public bool FailAfterCancellation { get; init; }
    }

    public sealed class NoOpHandler : IEventHandler<TestEvent>
    {
        public ValueTask HandleAsync(TestEvent message, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    public sealed class BlockingHandler(HandlerGate gate) : IEventHandler<TestEvent>
    {
        public async ValueTask HandleAsync(TestEvent message, CancellationToken cancellationToken)
        {
            gate.Started.TrySetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                gate.Cancelled = true;
                if (gate.FailAfterCancellation)
                {
                    throw new InvalidOperationException("Handler failed after shutdown cancellation.");
                }

                throw;
            }
        }
    }

    public sealed class CompletingHandler(HandlerGate gate) : IEventHandler<TestEvent>
    {
        public async ValueTask HandleAsync(TestEvent message, CancellationToken cancellationToken)
        {
            // completes successfully once the endpoint starts stopping
            var stopping = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await using var registration = cancellationToken.Register(() => stopping.TrySetResult());
            gate.Started.TrySetResult();
            await stopping.Task;
        }
    }
}
