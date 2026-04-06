namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionMigrateCommandTests(NitroCommandFixture fixture)
    : FusionCommandTestBase(fixture), IDisposable
{
    private readonly string _tempDir = CreateTempDir();

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "fusion",
            "migrate",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Migrate Fusion configuration files.

            Usage:
              nitro fusion migrate <TARGET> [options]

            Arguments:
              <subgraph-config>  The migration target

            Options:
              -w, --working-directory <working-directory>  Set the working directory for the command
              -?, -h, --help                               Show help and usage information

            Example:
              nitro fusion migrate subgraph-config
            """);
    }

    [Fact]
    public async Task MissingRequiredOptions_ReturnsError()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "fusion",
            "migrate");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Required argument missing for command: 'migrate'.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Migrate_SubgraphConfig()
    {
        // arrange
        var sourceFile = Path.Combine(_tempDir, "subgraph-config.json");
        var targetFile = Path.Combine(_tempDir, "schema-settings.json");
        const string sourceContent =
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
            """;

        SetupGlobMatch([sourceFile]);
        SetupFile(sourceFile, sourceContent);
        var outputFile = SetupCreateFile(targetFile);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "migrate",
            "subgraph-config",
            "--working-directory",
            _tempDir);

        // assert
        Assert.Equal(0, result.ExitCode);

        var json = await File.ReadAllTextAsync(outputFile);
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
        var sourceFile = Path.Combine(_tempDir, "subgraph-config.json");
        var targetFile = Path.Combine(_tempDir, "schema-settings.json");
        const string sourceContent =
            """
            {
              "subgraph": "Order",
              "http": { "baseAddress": "http://localhost:5001/graphql" }
            }
            """;

        SetupGlobMatch([sourceFile]);
        SetupFile(sourceFile, sourceContent);
        SetupFile(targetFile, """{ "existing": true }""");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "migrate",
            "subgraph-config",
            "--working-directory",
            _tempDir);

        // assert
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task Migrate_SubgraphConfig_NoFilesFound_ReturnsError()
    {
        // arrange
        SetupGlobMatch([]);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "migrate",
            "subgraph-config",
            "--working-directory",
            _tempDir);

        // assert
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Migrate_SubgraphConfig_MissingSubgraph()
    {
        // arrange
        var sourceFile = Path.Combine(_tempDir, "subgraph-config.json");
        var targetFile = Path.Combine(_tempDir, "schema-settings.json");
        const string sourceContent =
            """
            {
              "http": { "baseAddress": "http://localhost:5001/graphql" }
            }
            """;

        SetupGlobMatch([sourceFile]);
        SetupFile(sourceFile, sourceContent);
        var outputFile = SetupCreateFile(targetFile);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "migrate",
            "subgraph-config",
            "--working-directory",
            _tempDir);

        // assert
        Assert.Equal(0, result.ExitCode);

        var json = await File.ReadAllTextAsync(outputFile);
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

    public void Dispose()
    {
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
