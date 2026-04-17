using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class ValidateClientCommandTests(NitroCommandFixture fixture) : ClientsCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "client",
            "validate",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Validate a client version.

            Usage:
              nitro client validate [options]

            Options:
              --client-id <client-id> (REQUIRED)              The ID of the client [env: NITRO_CLIENT_ID]
              --stage <stage> (REQUIRED)                      The name of the stage [env: NITRO_STAGE]
              --operations-file <operations-file> (REQUIRED)  The path to the json file with the operations [env: NITRO_OPERATIONS_FILE]
              --cloud-url <cloud-url>                         The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                             The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                                 The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                  Show help and usage information

            Example:
              nitro client validate \
                --client-id "<client-id>" \
                --stage "dev" \
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
            "validate",
            "--stage",
            Stage,
            "--client-id",
            ClientId,
            "--operations-file",
            OperationsFile);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run `nitro login`.
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
            "validate",
            "--client-id",
            ClientId,
            "--stage",
            Stage,
            "--operations-file",
            "nonexistent.json");

        // assert
        result.AssertError(
            """
            Operations file '/some/working/directory/nonexistent.json' does not exist.
            """);
    }

    [Fact]
    public async Task StartClientValidationThrows_ReturnsError()
    {
        // arrange
        SetupOperationsFile();
        SetupValidateClientMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "validate",
            "--stage",
            Stage,
            "--client-id",
            ClientId,
            "--operations-file",
            OperationsFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Validating client 'client-1' against stage 'dev'
            └── ✕ Failed to validate the client.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetStartClientValidationErrors))]
    public async Task StartClientValidationHasErrors_ReturnsError(
        IValidateClientVersion_ValidateClient_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupOperationsFile();
        SetupValidateClientMutation(error);

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "validate",
            "--stage",
            Stage,
            "--client-id",
            ClientId,
            "--operations-file",
            OperationsFile);

        // assert
        result.StdErr.MatchInlineSnapshot(expectedErrorMessage);
        result.StdOut.MatchInlineSnapshot(
            """
            Validating client 'client-1' against stage 'dev'
            └── ✕ Failed to validate the client.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task StartClientValidationReturnsNullRequestId_ReturnsError()
    {
        // arrange
        SetupOperationsFile();
        SetupValidateClientMutationNullRequestId();

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "validate",
            "--stage",
            Stage,
            "--client-id",
            ClientId,
            "--operations-file",
            OperationsFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Could not create client validation request.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Validating client 'client-1' against stage 'dev'
            └── ✕ Failed to validate the client.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task ReturnsSuccess()
    {
        // arrange
        SetupOperationsFile();
        var capturedStream = SetupValidateClientMutation();
        SetupValidateClientSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "validate",
            "--client-id",
            ClientId,
            "--stage",
            Stage,
            "--operations-file",
            OperationsFile);

        // assert
        Assert.Equal("{}",
            System.Text.Encoding.UTF8.GetString(capturedStream.ToArray()));
        result.AssertSuccess(
            """
            Validating client 'client-1' against stage 'dev'
            ├── Validation request created. (ID: request-1)
            └── ✓ Validated client against stage 'dev'.
            """);
    }

    [Fact]
    public async Task WithEnvVars_ReturnsSuccess()
    {
        // arrange
        SetupOperationsFile();
        SetupEnvironmentVariable(EnvironmentVariables.ClientId, ClientId);
        SetupEnvironmentVariable(EnvironmentVariables.Stage, Stage);
        SetupEnvironmentVariable(EnvironmentVariables.OperationsFile, OperationsFile);

        SetupValidateClientMutation();
        SetupValidateClientSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "validate");

        // assert
        result.AssertSuccess(
            """
            Validating client 'client-1' against stage 'dev'
            ├── Validation request created. (ID: request-1)
            └── ✓ Validated client against stage 'dev'.
            """);
    }

    [Fact]
    public async Task BreakingChanges_ReturnsError()
    {
        // arrange
        SetupOperationsFile();
        SetupValidateClientMutation();
        SetupValidateClientSubscription(
            CreateClientVersionValidationFailedEventWithErrors());

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "validate",
            "--client-id",
            ClientId,
            "--stage",
            Stage,
            "--operations-file",
            OperationsFile);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating client 'client-1' against stage 'dev'
            ├── Validation request created. (ID: request-1)
            └── ✕ Client failed validation.
                └── Operation '6D12E4A815C50C504695E548EAF680BC8F337AC87E763E5689C685522A01BC59' (Deployed tags: 1.0.0)
                    └── foo (10:10)
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Client failed validation.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    public static TheoryData<IValidateClientVersion_ValidateClient_Errors, string>
        GetStartClientValidationErrors() => new()
    {
        {
            new ValidateClientVersion_ValidateClient_Errors_UnauthorizedOperation(
                "UnauthorizedOperation",
                "Not authorized to validate."),
            "Not authorized to validate."
        },
        {
            new ValidateClientVersion_ValidateClient_Errors_ClientNotFoundError(
                "Client not found.",
                "client-1"),
            "Client not found."
        },
        {
            new ValidateClientVersion_ValidateClient_Errors_StageNotFoundError(
                "StageNotFoundError",
                "Stage not found.",
                "dev"),
            "Stage not found."
        },
        {
            new ValidateClientVersion_ValidateClient_Errors_InvalidSourceMetadataInputError(
                "InvalidSourceMetadataInputError",
                "Invalid source metadata."),
            "Invalid source metadata."
        }
    };
}
