using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;
using Npgsql;

namespace Mocha.Transport.Postgres.Tests.Behaviors;

[Collection("Postgres")]
public class BusDefaultsIntegrationTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly PostgresFixture _fixture;

    public BusDefaultsIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ConfigureDefaults_Should_ProvisionQueues_When_BusStarts()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var db = await _fixture.CreateDatabaseAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddPostgres(t =>
            {
                t.ConnectionString(db.ConnectionString);
                t.ConfigureDefaults(d => d.Queue.AutoDelete = false);
            })
            .BuildTestBusAsync();

        // act - verify that queues were created in the database
        var queues = await ListQueuesAsync(db.ConnectionString);

        // assert - at least one application queue should exist after bus startup
        Assert.NotEmpty(queues);
    }

    [Fact]
    public async Task ConfigureDefaults_Should_DeliverMessages_When_DefaultsApplied()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var db = await _fixture.CreateDatabaseAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddPostgres(t =>
            {
                t.ConnectionString(db.ConnectionString);
                t.ConfigureDefaults(d =>
                {
                    d.Queue.AutoDelete = false;
                    d.Endpoint.MaxBatchSize = 5;
                });
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-DEFAULTS" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the event within timeout");

        var message = Assert.Single(recorder.Messages);
        var order = Assert.IsType<OrderCreated>(message);
        Assert.Equal("ORD-DEFAULTS", order.OrderId);
    }

    [Fact]
    public async Task ConfigureDefaults_Should_NotOverrideExplicitQueue_When_QueueDeclaredExplicitly()
    {
        // arrange - default sets AutoDelete=true, but explicit queue sets AutoDelete=false
        var capture = new OrderCapture();
        await using var db = await _fixture.CreateDatabaseAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(capture)
            .AddMessageBus()
            .AddConsumer<OrderSpyConsumer>()
            .AddPostgres(t =>
            {
                t.ConnectionString(db.ConnectionString);
                t.ConfigureDefaults(d => d.Queue.AutoDelete = true);
                t.BindHandlersExplicitly();

                // Explicitly declare a queue with AutoDelete=false - should override the default
                t.DeclareTopic("order-topic");
                t.DeclareQueue("explicit-q").AutoDelete(false);
                t.DeclareSubscription("order-topic", "explicit-q");

                t.Endpoint("explicit-ep").Consumer<OrderSpyConsumer>().Queue("explicit-q");
                t.DispatchEndpoint("order-dispatch").ToTopic("order-topic").Publish<OrderCreated>();
            })
            .BuildTestBusAsync();

        // act - verify the explicit queue exists in the database
        var queues = await ListQueuesAsync(db.ConnectionString);

        // assert - the explicit-q should be present (it was provisioned)
        Assert.Contains(queues, q => q == "explicit-q");
    }

    [Fact]
    public async Task ConfigureDefaults_Should_DeliverMessages_When_ExplicitQueueUsed()
    {
        // arrange
        var capture = new OrderCapture();
        await using var db = await _fixture.CreateDatabaseAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(capture)
            .AddMessageBus()
            .AddConsumer<OrderSpyConsumer>()
            .AddPostgres(t =>
            {
                t.ConnectionString(db.ConnectionString);
                t.ConfigureDefaults(d => d.Queue.AutoDelete = true);
                t.BindHandlersExplicitly();

                t.DeclareTopic("order-topic");
                t.DeclareQueue("override-q").AutoDelete(false);
                t.DeclareSubscription("order-topic", "override-q");

                t.Endpoint("override-ep").Consumer<OrderSpyConsumer>().Queue("override-q");
                t.DispatchEndpoint("order-dispatch").ToTopic("order-topic").Publish<OrderCreated>();
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-OVERRIDE" }, CancellationToken.None);

        // assert
        Assert.True(await capture.WaitAsync(s_timeout), "Consumer did not receive the message within timeout");

        var message = Assert.Single(capture.Messages);
        Assert.Equal("ORD-OVERRIDE", message.OrderId);
    }

    private static async Task<List<string>> ListQueuesAsync(string connectionString)
    {
        var schemaOptions = new PostgresSchemaOptions();
        var result = new List<string>();

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT name FROM {schemaOptions.QueueTable}";

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(reader.GetString(0));
        }

        return result;
    }

    public sealed class OrderCapture
    {
        private readonly SemaphoreSlim _semaphore = new(0);
        public ConcurrentBag<OrderCreated> Messages { get; } = [];

        public void Record(IConsumeContext<OrderCreated> context)
        {
            Messages.Add(context.Message);
            _semaphore.Release();
        }

        public async Task<bool> WaitAsync(TimeSpan timeout, int expectedCount = 1)
        {
            for (var i = 0; i < expectedCount; i++)
            {
                if (!await _semaphore.WaitAsync(timeout))
                {
                    return false;
                }
            }
            return true;
        }
    }

    public sealed class OrderSpyConsumer(OrderCapture capture) : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context)
        {
            capture.Record(context);
            return default;
        }
    }
}
