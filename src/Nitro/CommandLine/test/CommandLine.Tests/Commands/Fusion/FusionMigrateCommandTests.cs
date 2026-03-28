using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionMigrateCommandTests
{
    [Fact]
    public async Task Migrate_SubgraphConfig()
    {
        // arrange
        var workingDirectory = GetExistingWorkingDirectory();
        var subgraphConfigPath = Path.Combine(workingDirectory, "subgraph", "subgraph-config.json");
        var outputPath = Path.Combine(workingDirectory, "subgraph", "schema-settings.json");

        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [subgraphConfigPath] =
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
                """
        };

        var fileSystem = CreateFileSystem(files, [subgraphConfigPath]);
        var host = CreateHost(fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "fusion",
            "migrate",
            "subgraph-config",
            "--working-directory",
            workingDirectory);

        // assert
        Assert.Equal(0, exitCode);
        ReadText(fileSystem, outputPath).ReplaceLineEndings("\n").MatchInlineSnapshot(
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
        var workingDirectory = GetExistingWorkingDirectory();
        var subgraphConfigPath = Path.Combine(workingDirectory, "subgraph", "subgraph-config.json");
        var outputPath = Path.Combine(workingDirectory, "subgraph", "schema-settings.json");
        const string existingContent = """{ "existing": true }""";

        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [subgraphConfigPath] =
                """
                {
                  "subgraph": "Order",
                  "http": { "baseAddress": "http://localhost:5001/graphql" }
                }
                """,
            [outputPath] = existingContent
        };

        var fileSystem = CreateFileSystem(files, [subgraphConfigPath]);
        var host = CreateHost(fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "fusion",
            "migrate",
            "subgraph-config",
            "--working-directory",
            workingDirectory);

        // assert
        Assert.Equal(0, exitCode);
        Assert.Equal(existingContent, ReadText(fileSystem, outputPath));
    }

    [Fact]
    public async Task Migrate_SubgraphConfig_NoFilesFound_ReturnsError()
    {
        // arrange
        var workingDirectory = GetExistingWorkingDirectory();
        var fileSystem = CreateFileSystem(
            files: new Dictionary<string, string>(StringComparer.Ordinal),
            sourceFiles: []);
        var host = CreateHost(fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "fusion",
            "migrate",
            "subgraph-config",
            "--working-directory",
            workingDirectory);

        // assert
        Assert.Equal(-1, exitCode);
        Assert.Equal(
            $"Searching for 'subgraph-config.json' files in '{workingDirectory}'...\n✕ No subgraph-config.json files found.\n",
            host.Output.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task Migrate_SubgraphConfig_MissingSubgraph()
    {
        // arrange
        var workingDirectory = GetExistingWorkingDirectory();
        var subgraphConfigPath = Path.Combine(workingDirectory, "subgraph", "subgraph-config.json");
        var outputPath = Path.Combine(workingDirectory, "subgraph", "schema-settings.json");

        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [subgraphConfigPath] =
                """
                {
                  "http": { "baseAddress": "http://localhost:5001/graphql" }
                }
                """
        };

        var fileSystem = CreateFileSystem(files, [subgraphConfigPath]);
        var host = CreateHost(fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "fusion",
            "migrate",
            "subgraph-config",
            "--working-directory",
            workingDirectory);

        // assert
        Assert.Equal(0, exitCode);
        Assert.Equal(
            $"Searching for 'subgraph-config.json' files in '{workingDirectory}'...\n"
            + "subgraph/schema-settings.json needs to define a 'name'.\n"
            + "Migrated 1 file(s) to schema-settings.json!\n"
            + "subgraph/subgraph-config.json -> schema-settings.json\n",
            host.Output.ReplaceLineEndings("\n"));
        ReadText(fileSystem, outputPath).ReplaceLineEndings("\n").MatchInlineSnapshot(
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
        var workingDirectory = GetExistingWorkingDirectory();
        var subgraph1Path = Path.Combine(workingDirectory, "subgraph1", "subgraph-config.json");
        var subgraph2Path = Path.Combine(workingDirectory, "subgraph2", "subgraph-config.json");
        var outputPath1 = Path.Combine(workingDirectory, "subgraph1", "schema-settings.json");
        var outputPath2 = Path.Combine(workingDirectory, "subgraph2", "schema-settings.json");

        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [subgraph1Path] =
                """
                {
                  "subgraph": "Order",
                  "http": { "baseAddress": "http://localhost:5001/graphql" }
                }
                """,
            [subgraph2Path] =
                """
                {
                  "subgraph": "Product",
                  "http": { "baseAddress": "http://localhost:5002/graphql" }
                }
                """
        };

        var fileSystem = CreateFileSystem(files, [subgraph1Path, subgraph2Path]);
        var host = CreateHost(fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "fusion",
            "migrate",
            "subgraph-config",
            "--working-directory",
            workingDirectory);

        // assert
        Assert.Equal(0, exitCode);
        Assert.True(fileSystem.FileExists(outputPath1));
        Assert.True(fileSystem.FileExists(outputPath2));
    }

    private static CommandTestHost CreateHost(TestFileSystem fileSystem)
        => new CommandTestHost()
            .AddService<IFileSystem>(fileSystem)
            .AddService<ISessionService>(TestSessionService.WithWorkspace());

    private static string GetExistingWorkingDirectory()
        => Directory.Exists("/tmp")
            ? "/tmp"
            : Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    private static TestFileSystem CreateFileSystem(
        Dictionary<string, string> files,
        string[] sourceFiles)
    {
        var _ = sourceFiles;
        return new TestFileSystem(files.Select(x => new KeyValuePair<string, string>(x.Key, x.Value)).ToArray());
    }

    private static string ReadText(TestFileSystem fileSystem, string path)
        => fileSystem.ReadAllTextAsync(path, CancellationToken.None).GetAwaiter().GetResult();
}
