using System.Text;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionMigrateCommandTests(NitroCommandFixture fixture) : FusionCommandTestBase(fixture)
{
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
    public async Task MissingRequiredArgument_ReturnsError()
    {
        // arrange
        SetupNoAuthentication();

        // act
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
    public async Task Migrate_SubgraphConfig_NoFilesFound_ReturnsError()
    {
        // arrange
        SetupNoAuthentication();
        SetupGlobMatch([]);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "migrate",
            "subgraph-config");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Could not find any 'subgraph-config.json' files in '/some/working/directory'.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Migrate_SubgraphConfig_ReturnsSuccess()
    {
        // arrange
        SetupNoAuthentication();
        SetupFile(
            "subgraph-config.json",
            """
            {
              "subgraph": "Order",
              "http": {
                "clientName": "order-client",
                "baseAddress": "http://localhost:59093/graphql"
              },
              "websocket": { "baseAddress": "ws://localhost:59093/graphql" },
              "extensions": {
                "nitro": {
                  "apiId": "blah"
                }
              }
            }
            """);

        SetupGlobMatch(["subgraph-config.json"]);
        var capturedStream = SetupCreateFile("schema-settings.json");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "migrate",
            "subgraph-config");

        // assert
        result.AssertSuccess(
            """
            Searching for 'subgraph-config.json' files in '/some/working/directory'...
            Migrated 1 file(s) to schema-settings.json!
            /some/working/directory/subgraph-config.json -> schema-settings.json
            """);

        Encoding.UTF8.GetString(capturedStream.ToArray()).MatchInlineSnapshot(
            """
            {
              "version": "1.0.0",
              "name": "Order",
              "transports": {
                "http": {
                  "clientName": "order-client",
                  "url": "http://localhost:59093/graphql"
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
    public async Task Migrate_SubgraphConfig_SkipsIfTargetExists_ReturnsSuccess()
    {
        // arrange
        SetupNoAuthentication();
        SetupGlobMatch(["subgraph-config.json"]);
        SetupFile(
            "subgraph-config.json",
            """
            {
              "subgraph": "Order",
              "http": { "baseAddress": "http://localhost:5001/graphql" }
            }
            """);
        SetupFile("schema-settings.json", """{ "existing": true }""");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "migrate",
            "subgraph-config");

        // assert
        result.AssertSuccess(
            """
            Searching for 'subgraph-config.json' files in '/some/working/directory'...
            Skipping /some/working/directory/schema-settings.json (already exists)
            No files were migrated.
            """);
    }
}
