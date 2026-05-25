using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;
using Npgsql;

namespace Mocha.Transport.Postgres.Tests.Behaviors;

[Collection("Postgres")]
public class AutoProvisionIntegrationTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly PostgresFixture _fixture;

    public AutoProvisionIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PublishAsync_Should_Deliver_When_AutoProvisionEnabledByDefault()
    {
        // arrange - default auto-provision (true)
        var recorder = new MessageRecorder();
        await using var db = await _fixture.CreateDatabaseAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddPostgres(t => t.ConnectionString(db.ConnectionString))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "AP-1" }, CancellationToken.None);

        // assert - message is delivered because topology was auto-provisioned
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the event");
        var order = Assert.IsType<OrderCreated>(Assert.Single(recorder.Messages));
        Assert.Equal("AP-1", order.OrderId);
    }

    [Fact]
    public async Task PublishAsync_Should_Deliver_When_AutoProvisionExplicitlyEnabled()
    {
        // arrange - explicit auto-provision true
        var recorder = new MessageRecorder();
        await using var db = await _fixture.CreateDatabaseAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddPostgres(t =>
            {
                t.ConnectionString(db.ConnectionString);
                t.AutoProvision(true);
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "AP-2" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the event");
        var order = Assert.IsType<OrderCreated>(Assert.Single(recorder.Messages));
        Assert.Equal("AP-2", order.OrderId);
    }

    [Fact]
    public async Task SendAsync_Should_Deliver_When_AutoProvisionEnabledByDefault()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var db = await _fixture.CreateDatabaseAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddPostgres(t => t.ConnectionString(db.ConnectionString))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.SendAsync(new ProcessPayment { OrderId = "AP-3", Amount = 42.00m }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the request");
        var payment = Assert.IsType<ProcessPayment>(Assert.Single(recorder.Messages));
        Assert.Equal("AP-3", payment.OrderId);
    }

    [Fact]
    public async Task ExplicitTopology_Should_Deliver_When_AutoProvisionEnabledOnResources()
    {
        // arrange - transport auto-provision disabled, but individual resources enabled
        var capture = new OrderCapture();
        await using var db = await _fixture.CreateDatabaseAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(capture)
            .AddMessageBus()
            .AddConsumer<OrderSpyConsumer>()
            .AddPostgres(t =>
            {
                t.ConnectionString(db.ConnectionString);
                t.AutoProvision(false);
                t.BindHandlersExplicitly();
                t.DeclareTopic("ap-topic").AutoProvision(true);
                t.DeclareQueue("ap-q").AutoProvision(true);
                t.DeclareSubscription("ap-topic", "ap-q").AutoProvision(true);

                t.Endpoint("ap-ep").Consumer<OrderSpyConsumer>().Queue("ap-q");
                t.DispatchEndpoint("ap-dispatch").ToTopic("ap-topic").Publish<OrderCreated>();
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "AP-4" }, CancellationToken.None);

        // assert - resources were explicitly enabled, so message should be delivered
        Assert.True(await capture.WaitAsync(s_timeout), "Consumer did not receive the message");
        var message = Assert.Single(capture.Messages);
        Assert.Equal("AP-4", message.OrderId);
    }

    [Fact]
    public async Task ExplicitTopology_Should_Deliver_When_PreProvisionedAndAutoProvisionDisabled()
    {
        // arrange - pre-provision resources via SQL, then disable auto-provision
        var capture = new OrderCapture();
        await using var db = await _fixture.CreateDatabaseAsync();

        // Run schema migrations manually so tables exist
        var schemaOptions = new PostgresSchemaOptions();
        await using (var conn = new NpgsqlConnection(db.ConnectionString))
        {
            await conn.OpenAsync();
            var migrator = new PostgresSchemaMigrator(schemaOptions);
            await migrator.MigrateAsync(conn);
        }

        // Pre-provision topology resources directly in the database
        await using (var conn = new NpgsqlConnection(db.ConnectionString))
        {
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"""
                INSERT INTO {schemaOptions.TopicTable} (name) VALUES ('pre-topic');
                INSERT INTO {schemaOptions.QueueTable} (name) VALUES ('pre-q');
                INSERT INTO {schemaOptions.QueueSubscriptionTable} (source_id, destination_id)
                    SELECT t.id, q.id
                    FROM {schemaOptions.TopicTable} t, {schemaOptions.QueueTable} q
                    WHERE t.name = 'pre-topic' AND q.name = 'pre-q';
                """;
            await cmd.ExecuteNonQueryAsync();
        }

        await using var bus = await new ServiceCollection()
            .AddSingleton(capture)
            .AddMessageBus()
            .AddConsumer<OrderSpyConsumer>()
            .AddPostgres(t =>
            {
                t.ConnectionString(db.ConnectionString);
                t.AutoProvision(false);
                t.BindHandlersExplicitly();
                t.DeclareTopic("pre-topic");
                t.DeclareQueue("pre-q");
                t.DeclareSubscription("pre-topic", "pre-q");

                t.Endpoint("pre-ep").Consumer<OrderSpyConsumer>().Queue("pre-q");
                t.DispatchEndpoint("pre-dispatch").ToTopic("pre-topic").Publish<OrderCreated>();
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "AP-5" }, CancellationToken.None);

        // assert - pre-provisioned resources work with auto-provision disabled
        Assert.True(await capture.WaitAsync(s_timeout), "Consumer did not receive the message");
        var message = Assert.Single(capture.Messages);
        Assert.Equal("AP-5", message.OrderId);
    }

    [Fact]
    public async Task ExplicitTopology_Should_Deliver_When_MixedAutoProvision()
    {
        // arrange - transport enabled, some resources disabled but pre-provisioned
        var capture = new OrderCapture();
        await using var db = await _fixture.CreateDatabaseAsync();

        // Run schema migrations manually so tables exist
        var schemaOptions = new PostgresSchemaOptions();
        await using (var conn = new NpgsqlConnection(db.ConnectionString))
        {
            await conn.OpenAsync();
            var migrator = new PostgresSchemaMigrator(schemaOptions);
            await migrator.MigrateAsync(conn);
        }

        // Pre-provision only the topic (with auto-provision disabled for it)
        await using (var conn = new NpgsqlConnection(db.ConnectionString))
        {
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"INSERT INTO {schemaOptions.TopicTable} (name) VALUES ('mixed-topic')";
            await cmd.ExecuteNonQueryAsync();
        }

        await using var bus = await new ServiceCollection()
            .AddSingleton(capture)
            .AddMessageBus()
            .AddConsumer<OrderSpyConsumer>()
            .AddPostgres(t =>
            {
                t.ConnectionString(db.ConnectionString);
                t.AutoProvision(true);
                t.BindHandlersExplicitly();
                t.DeclareTopic("mixed-topic").AutoProvision(false); // already exists
                t.DeclareQueue("mixed-q"); // will be auto-provisioned (inherits true)
                t.DeclareSubscription("mixed-topic", "mixed-q"); // will be auto-provisioned

                t.Endpoint("mixed-ep").Consumer<OrderSpyConsumer>().Queue("mixed-q");
                t.DispatchEndpoint("mixed-dispatch").ToTopic("mixed-topic").Publish<OrderCreated>();
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "AP-6" }, CancellationToken.None);

        // assert
        Assert.True(await capture.WaitAsync(s_timeout), "Consumer did not receive the message");
        var message = Assert.Single(capture.Messages);
        Assert.Equal("AP-6", message.OrderId);
    }

    public sealed class ProcessPaymentHandler(MessageRecorder recorder) : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return default;
        }
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
