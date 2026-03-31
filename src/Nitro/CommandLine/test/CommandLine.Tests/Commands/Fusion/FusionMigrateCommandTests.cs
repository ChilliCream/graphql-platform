namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionMigrateCommandTests : IDisposable
{
    private readonly string _tempDir;

    public FusionMigrateCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "fusion",
                "migrate",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Migrate Fusion configuration files.

            Usage:
              nitro fusion migrate <TARGET> [options]

            Arguments:
              <subgraph-config>  The migration target.

            Options:
              -w, --working-directory <working-directory>  Sets the working directory for the command.
              --cloud-url <cloud-url>                      The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                          The API key that is used for the authentication [env: NITRO_API_KEY]
              --output <json>                              The output format. Setting this option will disable the interactive mode. [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                               Show help and usage information
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingRequiredOptions_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "migrate")
            .ExecuteAsync();

        // assert
        var output = result.StdOut.Replace(result.ExecutableName, "nitro");
        output.MatchInlineSnapshot(
            """
            Description:
              Migrate Fusion configuration files.

            Usage:
              nitro fusion migrate <TARGET> [options]

            Arguments:
              <subgraph-config>  The migration target.

            Options:
              -w, --working-directory <working-directory>  Sets the working directory for the command.
              --cloud-url <cloud-url>                      The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                          The API key that is used for the authentication [env: NITRO_API_KEY]
              --output <json>                              The output format. Setting this option will disable the interactive mode. [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                               Show help and usage information
            """);
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

        // act
        var result = await new CommandBuilder()
            .AddArguments(
                "fusion",
                "migrate",
                "subgraph-config",
                "--working-directory",
                _tempDir)
            .ExecuteAsync();

        // assert
        Assert.Equal(0, result.ExitCode);

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

        // act
        var result = await new CommandBuilder()
            .AddArguments(
                "fusion",
                "migrate",
                "subgraph-config",
                "--working-directory",
                _tempDir)
            .ExecuteAsync();

        // assert
        Assert.Equal(0, result.ExitCode);

        var json = await File.ReadAllTextAsync(Path.Combine(_tempDir, "schema-settings.json"));
        Assert.Equal(existingContent, json);
    }

    [Fact]
    public async Task Migrate_SubgraphConfig_NoFilesFound_ReturnsError()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "fusion",
                "migrate",
                "subgraph-config",
                "--working-directory",
                _tempDir)
            .ExecuteAsync();

        // assert
        Assert.Equal(1, result.ExitCode);
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

        // act
        var result = await new CommandBuilder()
            .AddArguments(
                "fusion",
                "migrate",
                "subgraph-config",
                "--working-directory",
                _tempDir)
            .ExecuteAsync();

        // assert
        Assert.Equal(0, result.ExitCode);
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

        // act
        var result = await new CommandBuilder()
            .AddArguments(
                "fusion",
                "migrate",
                "subgraph-config",
                "--working-directory",
                _tempDir)
            .ExecuteAsync();

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(Path.Combine(subDir1, "schema-settings.json")));
        Assert.True(File.Exists(Path.Combine(subDir2, "schema-settings.json")));
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
