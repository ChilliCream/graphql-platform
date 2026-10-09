using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection("RabbitMQ")]
public sealed class TemporaryEndpointCleanupTests(RabbitMQFixture fixture)
{
    [Fact]
    public async Task StopAsync_Should_RemoveTemporaryEndpointResources_When_StoppedGracefully()
    {
        // arrange
        const string queueName = "temporary-orders";
        var recorder = new MessageRecorder();
        await using var vhost = await fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ(t => t.Endpoint(queueName).Handler<OrderCreatedHandler>().Temporary())
            .BuildTestBusAsync();

        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = (RabbitMQMessagingTopology)transport.Topology;
        var binding = topology.Bindings
            .OfType<RabbitMQQueueBinding>()
            .Single(b => b.Destination.Name == queueName);
        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var result = await TemporaryEndpointCleanupScenario.ExecuteAsync(
            messageBus,
            recorder,
            ct => InspectResourcesAsync(vhost.VhostName, binding.Source.Name, queueName, ct),
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

    private async Task<TemporaryEndpointResourceState> InspectResourcesAsync(
        string vhostName,
        string sourceName,
        string queueName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var queues = await fixture.InvokeCommandAsync(
            ["rabbitmqctl", "list_queues", "name", "-p", vhostName, "--no-table-headers"]);
        var bindings = await fixture.InvokeCommandAsync(
            [
                "rabbitmqctl",
                "list_bindings",
                "source_name",
                "destination_name",
                "-p",
                vhostName,
                "--no-table-headers"
            ]);

        return new TemporaryEndpointResourceState(
            ContainsLine(queues, queueName),
            ContainsBinding(bindings, sourceName, queueName));
    }

    private static bool ContainsLine(string? value, string expected)
        => value?.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(expected, StringComparer.Ordinal) == true;

    private static bool ContainsBinding(string? value, string sourceName, string destinationName)
        => value?.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(sourceName + "\t" + destinationName, StringComparer.Ordinal) == true;
}
