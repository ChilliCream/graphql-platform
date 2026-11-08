using System.Text.Json;
using CookieCrumble.Resources;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;

namespace Mocha.Transport.RabbitMQ.Tests.Helpers;

public sealed class RabbitMQClusterFixture : IAsyncLifetime
{
    private readonly INetwork _network = new NetworkBuilder().Build();
    private readonly ClusterNode[] _nodes;

    public RabbitMQClusterFixture()
    {
        _nodes = Enumerable.Range(0, 3)
            .Select(index => new ClusterNode(_network, $"mocha-rabbit-{index}"))
            .ToArray();
    }

    public async ValueTask InitializeAsync()
    {
        await _network.CreateAsync(TestContext.Current.CancellationToken);
        foreach (var node in _nodes)
        {
            await node.InitializeAsync();
            if (!ReferenceEquals(node, _nodes[0]))
            {
                await node.InvokeCommandAsync(["rabbitmqctl", "stop_app"]);
                await node.InvokeCommandAsync(["rabbitmqctl", "reset"]);
                await node.InvokeCommandAsync(["rabbitmqctl", "join_cluster", GetNodeName(0)]);
                await node.InvokeCommandAsync(["rabbitmqctl", "start_app"]);
            }
        }
    }

    public string GetNodeName(int index) => $"rabbit@mocha-rabbit-{index}";

    public ConnectionFactory CreateConnectionFactory(int index)
        => new() { Uri = new Uri(_nodes[index].ConnectionString) };

    public async Task<IAsyncDisposable> KillNodeAsync(int index)
    {
        await _nodes[index].KillAsync();
        return new AsyncDisposable(() => _nodes[index].RestartAsync());
    }

    public async Task<QueueState?> GetQueueStateAsync(int node, string queue)
    {
        var output = await _nodes[node].InvokeCommandAsync(
            ["rabbitmqctl", "list_queues", "name", "leader", "members", "messages", "--formatter", "json", "--quiet"]);
        using var document = JsonDocument.Parse(output);
        foreach (var state in document.RootElement.EnumerateArray())
        {
            if (state.GetProperty("name").GetString() == queue)
            {
                return new QueueState(
                    state.GetProperty("leader").GetString()!,
                    state.GetProperty("members").EnumerateArray().Select(member => member.GetString()!).ToArray(),
                    state.GetProperty("messages").GetInt32());
            }
        }

        return null;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await Task.WhenAll(_nodes.Select(node => node.DisposeAsync().AsTask()));
        }
        finally
        {
            await _network.DisposeAsync();
        }
    }

    public sealed record QueueState(string Leader, string[] Members, int Messages);

    private sealed class ClusterNode(INetwork network, string hostname) : RabbitMQResource
    {
        public Task KillAsync() => Container.StopAsync(TestContext.Current.CancellationToken);

        public Task RestartAsync() => Container.StartAsync(TestContext.Current.CancellationToken);

        protected override RabbitMqContainer Build()
        {
#if RABBITMQ_3
            const string image = "rabbitmq:3.13";
#else
            const string image = "rabbitmq:4.3";
#endif
            return new RabbitMqBuilder(image)
                .WithUsername("guest")
                .WithPassword("guest")
                .WithNetwork(network)
                .WithNetworkAliases(hostname)
                .WithHostname(hostname)
                .WithEnvironment("RABBITMQ_ERLANG_COOKIE", "mocha-cluster-tests")
                .WithCreateParameterModifier(parameters => parameters.StopSignal = "SIGKILL")
                .Build();
        }
    }
}
