namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

// TODO: Overhaul these
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
    public async Task MissingRequiredOptions_ReturnsError()
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

    // [Fact]
    // public async Task Migrate_SubgraphConfig()
    // {
    //     // arrange
    //     var sourceFile = Path.Combine(_tempDir, "subgraph-config.json");
    //     var targetFile = Path.Combine(_tempDir, "schema-settings.json");
    //     const string sourceContent =
    //         """
    //         {
    //           "subgraph": "Order",
    //           "http": {
    //             "clientName": "order-client",
    //             "baseAddress": "http://localhost:59093/graphql",
    //             "timeout": 30
    //           },
    //           "websocket": { "baseAddress": "ws://localhost:59093/graphql" },
    //           "extensions": {
    //             "nitro": {
    //               "apiId": "blah"
    //             }
    //           }
    //         }
    //         """;
    //
    //     SetupGlobMatch([sourceFile]);
    //     SetupFile(sourceFile, sourceContent);
    //     var outputFile = SetupCreateFile(targetFile);
    //
    //     // act
    //     var result = await ExecuteCommandAsync(
    //         "fusion",
    //         "migrate",
    //         "subgraph-config",
    //         "--working-directory",
    //         _tempDir);
    //
    //     // assert
    //     Assert.Equal(0, result.ExitCode);
    //
    //     var json = await File.ReadAllTextAsync(outputFile);
    //     json.MatchInlineSnapshot(
    //         """
    //         {
    //           "version": "1.0.0",
    //           "name": "Order",
    //           "transports": {
    //             "http": {
    //               "clientName": "order-client",
    //               "url": "http://localhost:59093/graphql",
    //               "timeout": 30
    //             }
    //           },
    //           "extensions": {
    //             "nitro": {
    //               "apiId": "blah"
    //             }
    //           }
    //         }
    //         """);
    // }
    //
    // [Fact]
    // public async Task Migrate_SubgraphConfig_SkipsIfTargetExists()
    // {
    //     // arrange
    //     var sourceFile = Path.Combine(_tempDir, "subgraph-config.json");
    //     var targetFile = Path.Combine(_tempDir, "schema-settings.json");
    //     const string sourceContent =
    //         """
    //         {
    //           "subgraph": "Order",
    //           "http": { "baseAddress": "http://localhost:5001/graphql" }
    //         }
    //         """;
    //
    //     SetupGlobMatch([sourceFile]);
    //     SetupFile(sourceFile, sourceContent);
    //     SetupFile(targetFile, """{ "existing": true }""");
    //
    //     // act
    //     var result = await ExecuteCommandAsync(
    //         "fusion",
    //         "migrate",
    //         "subgraph-config",
    //         "--working-directory",
    //         _tempDir);
    //
    //     // assert
    //     Assert.Equal(0, result.ExitCode);
    // }
}
