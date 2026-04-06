using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.OpenApi;

public sealed class CreateOpenApiCollectionCommandTests(NitroCommandFixture fixture) : OpenApiCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "openapi",
            "create",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Create a new OpenAPI collection.

            Usage:
              nitro openapi create [options]

            Options:
                            --api-id <api-id>        The ID of the API [env: NITRO_API_ID]
                            --name <name>            The name of the OpenAPI collection [env: NITRO_OPENAPI_COLLECTION_NAME]
                            --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
                            --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
                            --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
                            -?, -h, --help           Show help and usage information

            Example:
              nitro openapi create \
                --name "my-collection" \
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
            "openapi",
            "create",
            "--api-id",
            ApiId,
            "--name",
            OpenApiCollectionName);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingNameOption_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "create",
            "--api-id",
            ApiId);

        // assert
        result.AssertError(
            """
            Missing required option '--name'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoWorkspaceInSession_And_NoApiId_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupSession();
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "create",
            "--name",
            OpenApiCollectionName);

        // assertÎ
        result.AssertError(
            """
            You are not logged in. Run `[bold blue]nitro login[/]` to sign in or manually specify the '--workspace-id' option (if available).
            """);
    }

    [Fact]
    public async Task MissingRequiredOptions_PromptsUser_ReturnsSuccess()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);
        SetupSelectApisPrompt((ApiId, "products"));
        SetupCreateOpenApiCollectionMutation();

        // act
        var command = StartInteractiveCommand(
            "openapi",
            "create");

        command.SelectOption(0); // API
        command.Input(OpenApiCollectionName); // Name
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task WithOptions_ReturnsSuccess_NonInteractive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.NonInteractive);
        SetupCreateOpenApiCollectionMutation();

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "create",
            "--api-id",
            ApiId,
            "--name",
            OpenApiCollectionName);

        // assert
        result.AssertSuccess(
            """
            Creating OpenAPI collection 'my-openapi' for API 'api-1'
            └── ✓ Created OpenAPI collection 'my-openapi'.

            {
              "id": "oa-1",
              "name": "my-openapi"
            }
            """);
    }

    [Fact]
    public async Task WithOptions_ReturnsSuccess_JsonOutput()
    {
        // arrange
        SetupInteractionMode(InteractionMode.JsonOutput);
        SetupCreateOpenApiCollectionMutation();

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "create",
            "--api-id",
            ApiId,
            "--name",
            OpenApiCollectionName);

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
    public async Task CreateOpenApiCollectionReturnsNullResult_ReturnsError()
    {
        // arrange
        SetupCreateOpenApiCollectionMutationNullResult();

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "create",
            "--api-id",
            ApiId,
            "--name",
            OpenApiCollectionName);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating OpenAPI collection 'my-openapi' for API 'api-1'
            └── ✕ Failed to create the OpenAPI collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the expected data.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(CreateMutationErrorCases))]
    public async Task CreateOpenApiCollectionHasErrors_ReturnsError(
        ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        SetupCreateOpenApiCollectionMutation(mutationError);

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "create",
            "--api-id",
            ApiId,
            "--name",
            OpenApiCollectionName);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating OpenAPI collection 'my-openapi' for API 'api-1'
            └── ✕ Failed to create the OpenAPI collection.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task CreateOpenApiCollectionThrows_ReturnsError()
    {
        // arrange
        SetupCreateOpenApiCollectionMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "create",
            "--api-id",
            ApiId,
            "--name",
            OpenApiCollectionName);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating OpenAPI collection 'my-openapi' for API 'api-1'
            └── ✕ Failed to create the OpenAPI collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    public static TheoryData<ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection_Errors, string>
        CreateMutationErrorCases =>
        new()
        {
            {
                new CreateOpenApiCollectionCommandMutation_CreateOpenApiCollection_Errors_ApiNotFoundError(
                    "API not found", "ApiNotFoundError", "api-1"),
                """
                API not found
                """
            },
            {
                new CreateOpenApiCollectionCommandMutation_CreateOpenApiCollection_Errors_UnauthorizedOperation(
                    "Not authorized", "UnauthorizedOperation"),
                """
                Not authorized
                """
            }
        };
}
