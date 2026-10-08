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
        await using var provider = CreateProvider(db.ConnectionString);
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
    public async Task StopAsync_Should_LeaveInterruptedMessageLeased_When_HostStops(bool failAfterCancellation)
    {
        // arrange
        await using var db = await fixture.CreateDatabaseAsync($"shutdown_{failAfterCancellation}");
        var gate = new HandlerGate(failAfterCancellation);
        var logs = new LogRecorder();
        await using var provider = new ServiceCollection()
            .AddSingleton(gate)
            .AddLogging(b => b.AddProvider(logs))
            .AddMessageBus()
            .AddEventHandler<BlockingHandler>()
            .AddPostgres(t =>
            {
                t.ConnectionString(db.ConnectionString);
                t.Endpoint("shutdown").Handler<BlockingHandler>().MaxConcurrency(1);
            })
            .Services.BuildServiceProvider();
        var services = provider.GetServices<IHostedService>().ToArray();
        foreach (var service in services)
        {
            await service.StartAsync(TestContext.Current.CancellationToken);
        }

        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMessageBus>()
            .PublishAsync(new TestEvent(), TestContext.Current.CancellationToken);
        await gate.Started.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // act
        foreach (var service in services.Reverse())
        {
            await service.StopAsync(CancellationToken.None)
                .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        }

        // assert
        await using var connection = new NpgsqlConnection(db.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT consumer_id IS NOT NULL, error_reason FROM mocha_message";
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        var messages = new List<(bool Leased, string? ErrorReason)>();
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            messages.Add((reader.GetBoolean(0), reader.IsDBNull(1) ? null : reader.GetString(1)));
        }

        Assert.True(gate.Cancelled);
        var message = Assert.Single(messages);
        Assert.True(message.Leased);
        Assert.Null(message.ErrorReason);
        Assert.Empty(logs.Entries);
    }

    private static ServiceProvider CreateProvider(string connectionString)
        => new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<NoOpHandler>()
            .AddPostgres(t =>
            {
                t.ConnectionString(connectionString);
                t.Endpoint("shutdown").Handler<NoOpHandler>();
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
            await service.StopAsync(TestContext.Current.CancellationToken);
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

    public sealed class TestEvent;

    public sealed class HandlerGate(bool failAfterCancellation)
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Cancelled { get; set; }
        public bool FailAfterCancellation => failAfterCancellation;
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
}
