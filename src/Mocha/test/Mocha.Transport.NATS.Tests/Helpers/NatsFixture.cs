using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using NATS.Client.Core;

namespace Mocha.Transport.NATS.Tests.Helpers;

public sealed class NatsFixture : IAsyncLifetime
{
    private IContainer _container = null!;

    public string ConnectionString { get; private set; } = "nats://localhost:4222";

    public async Task InitializeAsync()
    {
        _container = new ContainerBuilder()
            .WithImage("nats:latest")
            .WithCommand("-js")
            .WithPortBinding(4222, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Server is ready"))
            .Build();

        await _container.StartAsync();

        var mappedPort = _container.GetMappedPublicPort(4222);
        ConnectionString = $"nats://localhost:{mappedPort}";
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public NatsOpts CreateOptions(string? testName = null) =>
        new() { Url = ConnectionString, Name = testName ?? "test" };
}

[CollectionDefinition("NATS")]
public class NatsCollection : ICollectionFixture<NatsFixture>;
