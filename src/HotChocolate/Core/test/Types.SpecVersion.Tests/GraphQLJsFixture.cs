using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using IOPath = System.IO.Path;

namespace HotChocolate.Types.SpecVersion;

public sealed class GraphQLJsFixture : IAsyncLifetime
{
    private const string ContainerDirectory = "/workspace";
    private IContainer? _container;

    public string? SkipReason { get; private set; }

    public async ValueTask InitializeAsync()
    {
        var nodeFiles = Directory.GetFiles(
            IOPath.Combine(AppContext.BaseDirectory, "node"));

        try
        {
            var builder = new ContainerBuilder("node:24-alpine")
                .WithWorkingDirectory(ContainerDirectory)
                .WithEntrypoint("sh")
                .WithCommand("-c", "while true; do sleep 3600; done");

            foreach (var nodeFile in nodeFiles)
            {
                builder = builder.WithResourceMapping(new FileInfo(nodeFile), ContainerDirectory);
            }

            _container = builder.Build();

            await _container.StartAsync();
        }
        catch (Exception ex)
        {
            SkipReason = "Docker is unavailable or the node container could not start: " + ex.Message;
            return;
        }

        var result = await _container.ExecAsync(
            ["npm", "ci", "--no-audit", "--no-fund"],
            TestContext.Current.CancellationToken);

        if (result.ExitCode is not 0)
        {
            Assert.True(
                result.ExitCode is 0,
                "Unable to install graphql-js packages: " + result.Stderr);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    public void SkipWhenUnavailable()
    {
        if (SkipReason is not null)
        {
            Assert.Skip(SkipReason);
        }
    }

    public async Task<ValidationResult> ValidateAsync(string alias, string schema)
    {
        var schemaPath = ContainerDirectory + "/schema-" + Guid.NewGuid().ToString("N") + ".graphql";

        await _container!.CopyAsync(
            Encoding.UTF8.GetBytes(schema),
            schemaPath,
            ct: TestContext.Current.CancellationToken);

        var result = await _container.ExecAsync(
            ["node", "validate.js", alias, schemaPath],
            TestContext.Current.CancellationToken);

        return new ValidationResult(result.ExitCode is 0, result.Stdout, result.Stderr);
    }
}
