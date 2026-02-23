using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;

namespace Mocha.Transport.RabbitMQ.Tests.Helpers;

public sealed class RabbitMQFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder().WithImage("rabbitmq:4-alpine").Build();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public string ConnectionString => _container.GetConnectionString();

    public async Task<VhostContext> CreateVhostAsync(
        [CallerMemberName] string testName = "",
        [CallerFilePath] string filePath = "")
    {
        var vhostName = GenerateVhostName(testName, filePath);
        await _container.ExecAsync(["rabbitmqctl", "add_vhost", vhostName]);
        await _container.ExecAsync(["rabbitmqctl", "set_permissions", "-p", vhostName, "rabbitmq", ".*", ".*", ".*"]);
        return new VhostContext(this, vhostName);
    }

    internal async Task CloseAllConnectionsAsync(string reason = "test")
    {
        await _container.ExecAsync(["rabbitmqctl", "close_all_connections", reason]);
    }

    internal async Task DeleteVhostAsync(string vhostName)
    {
        await _container.ExecAsync(["rabbitmqctl", "delete_vhost", vhostName]);
    }

    private static string GenerateVhostName(string testName, string filePath)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(filePath)))[..8];
        return $"{testName}_{hash}";
    }
}

public sealed class VhostContext(RabbitMQFixture fixture, string vhostName) : IAsyncDisposable
{
    public IConnectionFactory ConnectionFactory { get; } =
        new ConnectionFactory { Uri = new Uri(fixture.ConnectionString), VirtualHost = vhostName };

    public async ValueTask DisposeAsync() => await fixture.DeleteVhostAsync(vhostName);
}

[CollectionDefinition("RabbitMQ")]
public class RabbitMQCollection : ICollectionFixture<RabbitMQFixture>;
