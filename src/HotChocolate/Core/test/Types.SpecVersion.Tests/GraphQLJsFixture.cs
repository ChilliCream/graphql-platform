using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace HotChocolate.Types.SpecVersion;

public sealed class GraphQLJsFixture : IAsyncLifetime
{
    private const string ContainerDirectory = "/workspace";
    private readonly string _temporaryDirectory = global::System.IO.Path.Combine(
        global::System.IO.Path.GetTempPath(),
        "hotchocolate-graphql-js-" + Guid.NewGuid().ToString("N"));
    private IContainer? _container;

    public string? SkipReason { get; private set; }

    public async ValueTask InitializeAsync()
    {
        CopyNodeFiles();

        try
        {
            _container = new ContainerBuilder("node:24-alpine")
                .WithBindMount(_temporaryDirectory, ContainerDirectory)
                .WithWorkingDirectory(ContainerDirectory)
                .WithEntrypoint("sh")
                .WithCommand("-c", "while true; do sleep 3600; done")
                .Build();

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

        if (Directory.Exists(_temporaryDirectory))
        {
            Directory.Delete(_temporaryDirectory, recursive: true);
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
        var fileName = "schema-" + Guid.NewGuid().ToString("N") + ".graphql";
        var schemaPath = global::System.IO.Path.Combine(_temporaryDirectory, fileName);
        await File.WriteAllTextAsync(schemaPath, schema, TestContext.Current.CancellationToken);

        var result = await _container!.ExecAsync(
            [
                "node",
                "validate.js",
                alias,
                global::System.IO.Path.Combine(ContainerDirectory, fileName)
            ],
            TestContext.Current.CancellationToken);

        return new ValidationResult(result.ExitCode is 0, result.Stdout, result.Stderr);
    }

    private void CopyNodeFiles()
    {
        var sourceDirectory = global::System.IO.Path.Combine(AppContext.BaseDirectory, "node");
        Directory.CreateDirectory(_temporaryDirectory);

        foreach (var sourcePath in Directory.GetFiles(sourceDirectory))
        {
            var destinationPath = global::System.IO.Path.Combine(
                _temporaryDirectory,
                global::System.IO.Path.GetFileName(sourcePath));
            File.Copy(sourcePath, destinationPath);
        }
    }
}

public sealed record ValidationResult(bool IsSuccess, string StandardOutput, string StandardError);
