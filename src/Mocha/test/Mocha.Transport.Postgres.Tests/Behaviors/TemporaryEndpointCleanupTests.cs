using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;
using Npgsql;

namespace Mocha.Transport.Postgres.Tests.Behaviors;

[Collection("Postgres")]
public sealed class TemporaryEndpointCleanupTests(PostgresFixture fixture)
{
    [Fact]
    public async Task StopAsync_Should_RemoveTemporaryEndpointResources_When_StoppedGracefully()
    {
        // arrange
        const string queueName = "temporary-orders";
        var recorder = new MessageRecorder();
        await using var db = await fixture.CreateDatabaseAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddPostgres(t =>
            {
                t.ConnectionString(db.ConnectionString);
                t.Endpoint(queueName).Handler<OrderCreatedHandler>().Temporary();
            })
            .BuildTestBusAsync();

        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var result = await TemporaryEndpointCleanupScenario.ExecuteAsync(
            messageBus,
            recorder,
            ct => InspectResourcesAsync(db.ConnectionString, queueName, ct),
            ct => transport.StopAsync(runtime, ct),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "MessageDelivered": true,
              "BeforeStop": {
                "QueueExists": true,
                "BindingExists": true
              },
              "AfterStop": {
                "QueueExists": false,
                "BindingExists": false
              }
            }
            """);
    }

    private static async Task<TemporaryEndpointResourceState> InspectResourcesAsync(
        string connectionString,
        string queueName,
        CancellationToken cancellationToken)
    {
        var schema = new PostgresSchemaOptions();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
            SELECT
                EXISTS(
                    SELECT 1
                    FROM {schema.QueueTable} q
                    WHERE q.name = @queue_name),
                EXISTS(
                    SELECT 1
                    FROM {schema.QueueSubscriptionTable} s
                    INNER JOIN {schema.QueueTable} q ON q.id = s.destination_id
                    WHERE q.name = @queue_name)
            """;
        command.Parameters.AddWithValue("queue_name", queueName);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return new TemporaryEndpointResourceState(reader.GetBoolean(0), reader.GetBoolean(1));
    }
}
