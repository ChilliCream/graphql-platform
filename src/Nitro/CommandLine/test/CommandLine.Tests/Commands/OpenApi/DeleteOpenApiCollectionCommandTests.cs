using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.OpenApi;

public sealed class DeleteOpenApiCollectionCommandTests(NitroCommandFixture fixture) : OpenApiCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "openapi",
            "delete",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Delete an OpenAPI collection.

            Usage:
              nitro openapi delete [<id>] [options]

            Arguments:
              <id>  The resource ID

            Options:
              --force                  Skip confirmation prompts for deletes and overwrites
              --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information

            Example:
              nitro openapi delete "<openapi-collection-id>"
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
            "openapi",
            "delete",
            OpenApiCollectionId,
            "--force");

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingRequiredId_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        SetupInteractionMode(mode);

        var result = await ExecuteCommandAsync(
            "openapi",
            "delete",
            "--force");

        // assert
        result.AssertError(
            """
            The 'id' option is required in non-interactive mode.
            """);
    }

    [Fact]
    public async Task WithoutForce_And_ConfirmationRejected_ReturnsError()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand(
            "openapi",
            "delete",
            OpenApiCollectionId);

        // act
        command.Confirm(false);
        var result = await command.RunToCompletionAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The OpenAPI collection was not deleted.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithForce_ReturnsSuccess_NonInteractive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.NonInteractive);
        SetupDeleteOpenApiCollectionMutation();

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "delete",
            OpenApiCollectionId,
            "--force");

        // assert
        result.AssertSuccess(
            """
            Deleting OpenAPI collection 'oa-1'
            └── ✓ Deleted OpenAPI collection 'oa-1'.

            {
              "id": "oa-1",
              "name": "my-openapi"
            }
            """);
    }

    [Fact]
    public async Task WithForce_ReturnsSuccess_JsonOutput()
    {
        // arrange
        SetupInteractionMode(InteractionMode.JsonOutput);
        SetupDeleteOpenApiCollectionMutation();

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "delete",
            OpenApiCollectionId,
            "--force");

        // assert
        result.AssertSuccess(
            """
            {
              "id": "oa-1",
              "name": "my-openapi"
            }
            """);
    }

    [Fact]
    public async Task WithConfirmation_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);
        SetupDeleteOpenApiCollectionMutation();

        var command = StartInteractiveCommand(
            "openapi",
            "delete",
            OpenApiCollectionId);

        // act
        command.Confirm(true);
        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task DeleteOpenApiCollectionReturnsNullResult_ReturnsError()
    {
        // arrange
        SetupDeleteOpenApiCollectionMutationNullResult();

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "delete",
            OpenApiCollectionId,
            "--force");

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Deleting OpenAPI collection 'oa-1'
            └── ✕ Failed to delete the OpenAPI collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the expected data.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(DeleteMutationErrorCases))]
    public async Task DeleteOpenApiCollectionHasErrors_ReturnsError(
        IDeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        SetupDeleteOpenApiCollectionMutation(mutationError);

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "delete",
            OpenApiCollectionId,
            "--force");

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Deleting OpenAPI collection 'oa-1'
            └── ✕ Failed to delete the OpenAPI collection.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task DeleteOpenApiCollectionThrows_ReturnsError()
    {
        // arrange
        SetupDeleteOpenApiCollectionMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "delete",
            OpenApiCollectionId,
            "--force");

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Deleting OpenAPI collection 'oa-1'
            └── ✕ Failed to delete the OpenAPI collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    public static TheoryData<IDeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById_Errors, string> DeleteMutationErrorCases =>
        new()
        {
            {
                new DeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById_Errors_OpenApiCollectionNotFoundError(
                    "OpenAPI collection not found", "oa-1"),
                """
                OpenAPI collection not found
                """
            },
            {
                new DeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById_Errors_UnauthorizedOperation(
                    "Not authorized", "UnauthorizedOperation"),
                """
                Not authorized
                """
            }
        };
}
