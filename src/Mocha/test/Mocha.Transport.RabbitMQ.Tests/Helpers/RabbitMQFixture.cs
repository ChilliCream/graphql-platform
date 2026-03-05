using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Docker.DotNet.Models;
using RabbitMQ.Client;
using Squadron;

namespace Mocha.Transport.RabbitMQ.Tests.Helpers;

public class MochaRabbitMQResource : RabbitMQResource
{
    public Task<string?> InvokeCommandAsync(string[] command)
        => Manager.InvokeCommandAsync(
            new ContainerExecCreateParameters
            {
                Cmd = command,
                AttachStdout = true,
                AttachStderr = true
            });
}

public sealed class RabbitMQFixture : IAsyncLifetime
{
    private readonly MochaRabbitMQResource _resource = new();

    public async Task InitializeAsync()
    {
        await _resource.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _resource.DisposeAsync();
    }

    public string ConnectionString => _resource.ConnectionString;

    public async Task<VhostContext> CreateVhostAsync(
        [CallerMemberName] string testName = "",
        [CallerFilePath] string filePath = "")
    {
        var vhostName = GenerateVhostName(testName, filePath);
        await _resource.InvokeCommandAsync(["rabbitmqctl", "add_vhost", vhostName]);
        await _resource.InvokeCommandAsync(["rabbitmqctl", "set_permissions", "-p", vhostName, "guest", ".*", ".*", ".*"]);
        return new VhostContext(this, vhostName);
    }

    internal async Task CloseAllConnectionsAsync(string reason = "test")
    {
        await _resource.InvokeCommandAsync(["rabbitmqctl", "close_all_connections", reason]);
    }

    internal async Task DeleteVhostAsync(string vhostName)
    {
        await _resource.InvokeCommandAsync(["rabbitmqctl", "delete_vhost", vhostName]);
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
