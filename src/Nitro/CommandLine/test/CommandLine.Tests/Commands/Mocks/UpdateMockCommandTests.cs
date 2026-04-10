using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mocks;

public sealed class UpdateMockCommandTests(NitroCommandFixture fixture) : MocksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "mock",
            "update",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Update a mock schema with a new schema and extension file.

            Usage:
              nitro mock update [<id>] [options]

            Arguments:
              <id>  The resource ID

            Options:
              --extension <extension>  The path to the graphql file with the schema extension [env: NITRO_SCHEMA_EXTENSION_FILE]
              --schema <schema>        The path to the graphql file with the schema [env: NITRO_SCHEMA_FILE]
              --url <url>              The URL of the downstream service [env: NITRO_DOWNSTREAM_URL]
              --name <name>            The name of the mock schema [env: NITRO_MOCK_SCHEMA_NAME]
              --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information

            Example:
              nitro mock update "<mock-schema-id>" \
                --extension "./extension.graphql" \
                --schema "./schema.graphqls"
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
            "update",
            MockSchemaId);

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
            "update",
            MockSchemaId,
            "--extension",
            "nonexistent.graphql");

        // assert
        result.AssertError(
            """
            Extension file '/some/working/directory/nonexistent.graphql' does not exist.
            """);
    }

    [Fact]
    public async Task SchemaFileDoesNotExist_ReturnsError()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "mock",
            "update",
            MockSchemaId,
            "--schema",
            "nonexistent.graphql");

        // assert
        result.AssertError(
            """
            Schema file '/some/working/directory/nonexistent.graphql' does not exist.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingId_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "mock",
            "update");

        // assert
        result.AssertError(
            """
            The 'id' option is required in non-interactive mode.
            """);
    }

    [Fact]
    public async Task UpdateMockThrows_ReturnsError()
    {
        // arrange
        SetupUpdateMockMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "mock",
            "update",
            MockSchemaId);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Updating mock schema 'mock-1'
            └── ✕ Failed to update the mock schema.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task UpdateMockReturnsNullMockSchema_ReturnsError()
    {
        // arrange
        SetupUpdateMockMutationNullMockSchema();

        // act
        var result = await ExecuteCommandAsync(
            "mock",
            "update",
            MockSchemaId);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Updating mock schema 'mock-1'
            └── ✕ Failed to update the mock schema.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the expected data.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetUpdateMockErrors))]
    public async Task UpdateMockHasErrors_ReturnsError(
        IUpdateMockSchema_UpdateMockSchema_Errors error,
        string expectedStdErr)
    {
        // arrange
        SetupUpdateMockMutation(null, null, null, null, error);

        // act
        var result = await ExecuteCommandAsync(
            "mock",
            "update",
            MockSchemaId);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Updating mock schema 'mock-1'
            └── ✕ Failed to update the mock schema.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithAllOptions_ReturnsSuccess_NonInteractive()
    {
        // arrange
        SetupUpdateMockMutationWithFiles();

        // act
        var result = await ExecuteCommandAsync(
            "mock",
            "update",
            MockSchemaId,
            "--extension",
            ExtensionFile,
            "--schema",
            SchemaFile,
            "--url",
            DownstreamUrl,
            "--name",
            UpdatedMockSchemaName);

        // assert
        result.AssertSuccess(
            """
            Updating mock schema 'mock-1'
            └── ✓ Updated mock schema 'mock-1'.

            {
              "id": "mock-1",
              "name": "updated-mock",
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
    public async Task WithAllOptions_ReturnsSuccess_JsonOutput()
    {
        // arrange
        SetupUpdateMockMutationWithFiles();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "mock",
            "update",
            MockSchemaId,
            "--extension",
            ExtensionFile,
            "--schema",
            SchemaFile,
            "--url",
            DownstreamUrl,
            "--name",
            UpdatedMockSchemaName);

        // assert
        result.AssertSuccess(
            """
            {
              "id": "mock-1",
              "name": "updated-mock",
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
    public async Task WithIdOnly_NoOptionalFiles_ReturnsSuccess()
    {
        // arrange
        SetupUpdateMockMutation(null, null, null, null);

        // act
        var result = await ExecuteCommandAsync(
            "mock",
            "update",
            MockSchemaId);

        // assert
        result.AssertSuccess(
            """
            Updating mock schema 'mock-1'
            └── ✓ Updated mock schema 'mock-1'.

            {
              "id": "mock-1",
              "name": "updated-mock",
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
    public async Task WithNameOnly_ReturnsSuccess()
    {
        // arrange
        SetupUpdateMockMutation(null, null, null, "new-name");

        // act
        var result = await ExecuteCommandAsync(
            "mock",
            "update",
            MockSchemaId,
            "--name",
            "new-name");

        // assert
        result.AssertSuccess(
            """
            Updating mock schema 'mock-1'
            └── ✓ Updated mock schema 'mock-1'.

            {
              "id": "mock-1",
              "name": "updated-mock",
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
    public async Task WithUrlOnly_ReturnsSuccess()
    {
        // arrange
        SetupUpdateMockMutation(null, "https://new-downstream.example.com", null, null);

        // act
        var result = await ExecuteCommandAsync(
            "mock",
            "update",
            MockSchemaId,
            "--url",
            "https://new-downstream.example.com");

        // assert
        result.AssertSuccess(
            """
            Updating mock schema 'mock-1'
            └── ✓ Updated mock schema 'mock-1'.

            {
              "id": "mock-1",
              "name": "updated-mock",
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

    public static TheoryData<IUpdateMockSchema_UpdateMockSchema_Errors, string>
        GetUpdateMockErrors() => new()
    {
        { CreateUpdateMockNotFoundError(), "Mock schema not found" },
        { CreateUpdateMockNonUniqueNameError(), "Name already in use" },
        { CreateUpdateMockUnauthorizedError(), "Not authorized" },
        { CreateUpdateMockValidationError(), "Validation failed" }
    };
}
