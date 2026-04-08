using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class UploadClientCommandTests(NitroCommandFixture fixture) : ClientsCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "client",
            "upload",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Upload a new client version.

            Usage:
              nitro client upload [options]

            Options:
              --client-id <client-id> (REQUIRED)              The ID of the client [env: NITRO_CLIENT_ID]
              --tag <tag> (REQUIRED)                          The tag of the schema version to deploy [env: NITRO_TAG]
              --operations-file <operations-file> (REQUIRED)  The path to the json file with the operations [env: NITRO_OPERATIONS_FILE]
              --cloud-url <cloud-url>                         The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                             The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                                 The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                  Show help and usage information

            Example:
              nitro client upload \
                --client-id "<client-id>" \
                --tag "v1" \
                --operations-file ./operations.json
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        SetupInteractionMode(mode);
        SetupNoAuthentication();

        var result = await ExecuteCommandAsync(
            "client",
            "upload",
            "--tag",
            Tag,
            "--operations-file",
            OperationsFile,
            "--client-id",
            ClientId);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task OperationsFileDoesNotExist_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "upload",
            "--client-id",
            ClientId,
            "--tag",
            Tag,
            "--operations-file",
            "nonexistent.json");

        // assert
        result.AssertError(
            """
            Operations file '/some/working/directory/nonexistent.json' does not exist.
            """);
    }

    [Fact]
    public async Task UploadClientThrows_ReturnsError()
    {
        // arrange
        SetupOperationsFile();
        SetupUploadClientMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "upload",
            "--tag",
            Tag,
            "--operations-file",
            OperationsFile,
            "--client-id",
            ClientId);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading new client version 'v1' for client 'client-1'
            └── ✕ Failed to upload a new client version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetUploadClientErrors))]
    public async Task UploadClientHasErrors_ReturnsError(
        IUploadClient_UploadClient_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupOperationsFile();
        SetupUploadClientMutation(error);

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "upload",
            "--tag",
            Tag,
            "--operations-file",
            OperationsFile,
            "--client-id",
            ClientId);

        // assert
        result.StdErr.MatchInlineSnapshot(expectedErrorMessage);
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading new client version 'v1' for client 'client-1'
            └── ✕ Failed to upload a new client version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task UploadClientReturnsNullClientVersion_ReturnsError()
    {
        // arrange
        SetupOperationsFile();
        SetupUploadClientMutationNullClientVersion();

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "upload",
            "--tag",
            Tag,
            "--operations-file",
            OperationsFile,
            "--client-id",
            ClientId);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Could not upload client.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading new client version 'v1' for client 'client-1'
            └── ✕ Failed to upload a new client version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task UploadsClient_ReturnsSuccess()
    {
        // arrange
        SetupOperationsFile();
        var capturedStream = SetupUploadClientMutation();

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "upload",
            "--tag",
            Tag,
            "--operations-file",
            OperationsFile,
            "--client-id",
            ClientId);

        // assert
        Assert.Equal("{}",
            System.Text.Encoding.UTF8.GetString(capturedStream.ToArray()));
        result.AssertSuccess(
            """
            Uploading new client version 'v1' for client 'client-1'
            └── ✓ Uploaded new client version 'v1'.
            """);
    }

    public static TheoryData<IUploadClient_UploadClient_Errors, string> GetUploadClientErrors() => new()
    {
        {
            new UploadClient_UploadClient_Errors_UnauthorizedOperation(
                "UnauthorizedOperation",
                "Not authorized to upload."),
            "Not authorized to upload."
        },
        {
            new UploadClient_UploadClient_Errors_ClientNotFoundError(
                "Client not found.",
                "client-1"),
            "Client not found."
        },
        {
            new UploadClient_UploadClient_Errors_DuplicatedTagError(
                "DuplicatedTagError",
                "Tag 'v1' already exists."),
            "Tag 'v1' already exists."
        },
        {
            new UploadClient_UploadClient_Errors_ConcurrentOperationError(
                "ConcurrentOperationError",
                "A concurrent operation is in progress."),
            "A concurrent operation is in progress."
        },
        {
            new UploadClient_UploadClient_Errors_InvalidPersistedQueryError(
                "Invalid persisted query."),
            "Invalid persisted query."
        },
        {
            new UploadClient_UploadClient_Errors_InvalidSourceMetadataInputError(
                "InvalidSourceMetadataInputError",
                "Invalid source metadata."),
            "Invalid source metadata."
        }
    };
}
