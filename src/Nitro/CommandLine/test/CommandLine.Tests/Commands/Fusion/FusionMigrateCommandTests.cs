using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionMigrateCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _originalDirectory = Directory.GetCurrentDirectory();

    public FusionMigrateCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public async Task Migrate_SubgraphConfig()
    {
        // arrange
        await File.WriteAllTextAsync(
            Path.Combine(_tempDir, "subgraph-config.json"),
            """
            {
              "subgraph": "Order",
              "http": {
                "clientName": "order-client",
                "baseAddress": "http://localhost:59093/graphql",
                "timeout": 30
              },
              "websocket": { "baseAddress": "ws://localhost:59093/graphql" },
              "extensions": {
                "nitro": {
                  "apiId": "blah"
                }
              }
            }
            """);

        Directory.SetCurrentDirectory(_tempDir);
        var builder = GetCommandLineBuilder();

        // act
        var exitCode = await builder.Build().InvokeAsync(
            ["fusion", "migrate", "subgraph-config"]);

        // assert
        Assert.Equal(0, exitCode);

        var json = await File.ReadAllTextAsync(Path.Combine(_tempDir, "schema-settings.json"));
        json.MatchInlineSnapshot(
            """
            {
              "version": "1.0.0",
              "name": "Order",
              "transports": {
                "http": {
                  "clientName": "order-client",
                  "url": "http://localhost:59093/graphql",
                  "timeout": 30
                }
              },
              "extensions": {
                "nitro": {
                  "apiId": "blah"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Migrate_SubgraphConfig_SkipsIfTargetExists()
    {
        // arrange
        const string existingContent = """{ "existing": true }""";
        await File.WriteAllTextAsync(
            Path.Combine(_tempDir, "subgraph-config.json"),
            """
            {
              "subgraph": "Order",
              "http": { "baseAddress": "http://localhost:5001/graphql" }
            }
            """);
        await File.WriteAllTextAsync(
            Path.Combine(_tempDir, "schema-settings.json"),
            existingContent);

        Directory.SetCurrentDirectory(_tempDir);
        var builder = GetCommandLineBuilder();

        // act
        var exitCode = await builder.Build().InvokeAsync(
            ["fusion", "migrate", "subgraph-config"]);

        // assert
        Assert.Equal(0, exitCode);

        var json = await File.ReadAllTextAsync(Path.Combine(_tempDir, "schema-settings.json"));
        Assert.Equal(existingContent, json);
    }

    [Fact]
    public async Task Migrate_SubgraphConfig_NoFilesFound_ReturnsError()
    {
        // arrange
        Directory.SetCurrentDirectory(_tempDir);
        var builder = GetCommandLineBuilder();

        // act
        var exitCode = await builder.Build().InvokeAsync(
            ["fusion", "migrate", "subgraph-config"]);

        // assert
        Assert.Equal(-1, exitCode);
    }

    [Fact]
    public async Task Migrate_SubgraphConfig_MissingSubgraph()
    {
        // arrange
        await File.WriteAllTextAsync(
            Path.Combine(_tempDir, "subgraph-config.json"),
            """
            {
              "http": { "baseAddress": "http://localhost:5001/graphql" }
            }
            """);

        Directory.SetCurrentDirectory(_tempDir);
        var builder = GetCommandLineBuilder();

        // act
        var exitCode = await builder.Build().InvokeAsync(
            ["fusion", "migrate", "subgraph-config"]);

        // assert
        Assert.Equal(0, exitCode);
        var json = await File.ReadAllTextAsync(Path.Combine(_tempDir, "schema-settings.json"));
        json.MatchInlineSnapshot(
            """
            {
              "version": "1.0.0",
              "name": "",
              "transports": {
                "http": {
                  "url": "http://localhost:5001/graphql"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Migrate_SubgraphConfig_MultipleFiles()
    {
        // arrange
        var subDir1 = Path.Combine(_tempDir, "subgraph1");
        var subDir2 = Path.Combine(_tempDir, "subgraph2");
        Directory.CreateDirectory(subDir1);
        Directory.CreateDirectory(subDir2);

        await File.WriteAllTextAsync(
            Path.Combine(subDir1, "subgraph-config.json"),
            """
            {
              "subgraph": "Order",
              "http": { "baseAddress": "http://localhost:5001/graphql" }
            }
            """);

        await File.WriteAllTextAsync(
            Path.Combine(subDir2, "subgraph-config.json"),
            """
            {
              "subgraph": "Product",
              "http": { "baseAddress": "http://localhost:5002/graphql" }
            }
            """);

        Directory.SetCurrentDirectory(_tempDir);
        var builder = GetCommandLineBuilder();

        // act
        var exitCode = await builder.Build().InvokeAsync(
            ["fusion", "migrate", "subgraph-config"]);

        // assert
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(subDir1, "schema-settings.json")));
        Assert.True(File.Exists(Path.Combine(subDir2, "schema-settings.json")));
    }

    private static CommandLineBuilder GetCommandLineBuilder()
    {
        var rootCommand = new Command("nitro");
        rootCommand.AddNitroCloudCommands();
        return new CommandLineBuilder(rootCommand)
            .UseExtendedConsole()
            .UseExceptionMiddleware()
            .UseDefaults();
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_originalDirectory);

        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // ignore
        }
    }
}
