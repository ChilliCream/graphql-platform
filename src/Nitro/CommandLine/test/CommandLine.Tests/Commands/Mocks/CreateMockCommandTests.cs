using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mocks;

public sealed class CreateMockCommandTests(NitroCommandFixture fixture) : MocksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "mock",
            "create",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Create a new mock schema.

            Usage:
              nitro mock create [options]

            Options:
              --api-id <api-id>                   The ID of the API [env: NITRO_API_ID]
              --extension <extension> (REQUIRED)  The path to the graphql file with the schema extension [env: NITRO_SCHEMA_EXTENSION_FILE]
              --schema <schema> (REQUIRED)        The path to the graphql file with the schema [env: NITRO_SCHEMA_FILE]
              --url <url> (REQUIRED)              The URL of the downstream service [env: NITRO_DOWNSTREAM_URL]
              --name <name> (REQUIRED)            The name of the mock schema [env: NITRO_MOCK_SCHEMA_NAME]
              --cloud-url <cloud-url>             The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                 The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                     The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                      Show help and usage information

            Example:
              nitro mock create \
                --schema "./schema.graphqls" \
                --url "https://example.com/graphql" \
                --extension "./extension.graphql" \
                --name "my-mock" \
                --api-id "<api-id>"
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupNoAuthentication();

        // act
        var result = await ExecuteCommandAsync(
            "mock",
            "create",
            "--api-id",
            ApiId,
            "--extension",
            ExtensionFile,
            "--schema",
            SchemaFile,
            "--url",
            DownstreamUrl,
            "--name",
            MockSchemaName);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Fact]
    public async Task ExtensionFileDoesNotExist_ReturnsError()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "mock",
            "create",
            "--api-id",
            ApiId,
            "--extension",
            "nonexistent.graphql",
            "--schema",
            SchemaFile,
            "--url",
            DownstreamUrl,
            "--name",
            MockSchemaName);

        // assert
        result.AssertError(
            """
            Extension file '/some/working/directory/nonexistent.graphql' does not exist.
            """);
    }

    [Fact]
    public async Task SchemaFileDoesNotExist_ReturnsError()
    {
        // arrange
        SetupFile(ExtensionFile, "extension content");

        // act
        var result = await ExecuteCommandAsync(
            "mock",
            "create",
            "--api-id",
            ApiId,
            "--extension",
            ExtensionFile,
            "--schema",
            "nonexistent.graphql",
            "--url",
            DownstreamUrl,
            "--name",
            MockSchemaName);

        // assert
        result.AssertError(
            """
            Schema file '/some/working/directory/nonexistent.graphql' does not exist.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingRequiredOptions_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "mock",
            "create",
            "--api-id",
            ApiId,
            "--extension",
            ExtensionFile,
            "--schema",
            SchemaFile,
            "--url",
            DownstreamUrl);

        // assert
        Assert.Contains("Create a new mock schema.", result.StdOut);
        Assert.Contains("--name <name> (REQUIRED)", result.StdOut);
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--name' is required.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task CreateMockThrows_ReturnsError()
    {
        // arrange
        SetupCreateMockMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "mock",
            "create",
            "--api-id",
            ApiId,
            "--extension",
            ExtensionFile,
            "--schema",
            SchemaFile,
            "--url",
            DownstreamUrl,
            "--name",
            MockSchemaName);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating mock schema 'my-mock' for API 'api-1'
            └── ✕ Failed to create the mock schema.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task CreateMockReturnsNullMockSchema_ReturnsError()
    {
        // arrange
        SetupCreateMockMutationNullMockSchema();

        // act
        var result = await ExecuteCommandAsync(
            "mock",
            "create",
            "--api-id",
            ApiId,
            "--extension",
            ExtensionFile,
            "--schema",
            SchemaFile,
            "--url",
            DownstreamUrl,
            "--name",
            MockSchemaName);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating mock schema 'my-mock' for API 'api-1'
            └── ✕ Failed to create the mock schema.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the expected data.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetCreateMockErrors))]
    public async Task CreateMockHasErrors_ReturnsError(
        ICreateMockSchema_CreateMockSchema_Errors error,
        string expectedStdErr)
    {
        // arrange
        SetupCreateMockMutation(error);

        // act
        var result = await ExecuteCommandAsync(
            "mock",
            "create",
            "--api-id",
            ApiId,
            "--extension",
            ExtensionFile,
            "--schema",
            SchemaFile,
            "--url",
            DownstreamUrl,
            "--name",
            MockSchemaName);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating mock schema 'my-mock' for API 'api-1'
            └── ✕ Failed to create the mock schema.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithOptions_ReturnsSuccess_NonInteractive()
    {
        // arrange
        SetupCreateMockMutation();

        // act
        var result = await ExecuteCommandAsync(
            "mock",
            "create",
            "--api-id",
            ApiId,
            "--extension",
            ExtensionFile,
            "--schema",
            SchemaFile,
            "--url",
            DownstreamUrl,
            "--name",
            MockSchemaName);

        // assert
        result.AssertSuccess(
            """
            Creating mock schema 'my-mock' for API 'api-1'
            └── ✓ Created mock schema 'my-mock'.

            {
              "id": "mock-1",
              "name": "my-mock",
              "url": "https://mock.example.com",
              "downstreamUrl": "https://downstream.example.com/",
              "createdBy": {
                "username": "user1",
                "createdAt": "2025-01-15T10:00:00+00:00"
              },
              "modifiedBy": {
                "username": "user2",
                "modifiedAt": "2025-01-16T10:00:00+00:00"
              }
            }
            """);
    }

    [Fact]
    public async Task WithOptions_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupCreateMockMutation();
        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);

        // act
        var result = await ExecuteCommandAsync(
            "mock",
            "create",
            "--api-id",
            ApiId,
            "--extension",
            ExtensionFile,
            "--schema",
            SchemaFile,
            "--url",
            DownstreamUrl,
            "--name",
            MockSchemaName);

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task WithOptions_ReturnsSuccess_JsonOutput()
    {
        // arrange
        SetupCreateMockMutation();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "mock",
            "create",
            "--api-id",
            ApiId,
            "--extension",
            ExtensionFile,
            "--schema",
            SchemaFile,
            "--url",
            DownstreamUrl,
            "--name",
            MockSchemaName);

        // assert
        result.AssertSuccess(
            """
            {
              "id": "mock-1",
              "name": "my-mock",
              "url": "https://mock.example.com",
              "downstreamUrl": "https://downstream.example.com/",
              "createdBy": {
                "username": "user1",
                "createdAt": "2025-01-15T10:00:00+00:00"
              },
              "modifiedBy": {
                "username": "user2",
                "modifiedAt": "2025-01-16T10:00:00+00:00"
              }
            }
            """);
    }

    public static TheoryData<ICreateMockSchema_CreateMockSchema_Errors, string>
        GetCreateMockErrors() => new()
    {
        { CreateCreateMockApiNotFoundError(), "API not found" },
        { CreateCreateMockNonUniqueNameError(), "Name already in use" },
        { CreateCreateMockUnauthorizedError(), "Not authorized" },
        { CreateCreateMockValidationError(), "Validation failed" }
    };
}
