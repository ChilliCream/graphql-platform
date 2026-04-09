using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Apis;

public sealed class CreateApiCommandTests(NitroCommandFixture fixture) : ApisCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "api",
            "create",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Create a new API.

            Usage:
              nitro api create [options]

            Options:
              --path <path>                        The path to the API [env: NITRO_API_PATH]
              --name <name>                        The name of the API [env: NITRO_API_NAME]
              --workspace-id <workspace-id>        The ID of the workspace [env: NITRO_WORKSPACE_ID]
              --kind <collection|gateway|service>  The kind of the API [env: NITRO_API_KIND]
              --cloud-url <cloud-url>              The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                  The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                      The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                       Show help and usage information

            Example:
              nitro api create --name "my-api"
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
            "api",
            "create",
            "--workspace-id",
            WorkspaceId,
            "--name",
            ApiName,
            "--path",
            "/products");

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
    public async Task NoWorkspaceInSession_And_NoWorkspaceOption_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "create",
            "--name",
            ApiName,
            "--path",
            "/products");

        // assert
        result.AssertError(
            """
            You are not logged in. Run `[bold blue]nitro login[/]` to sign in or manually specify the '--workspace-id' option (if available).
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingRequiredOptions_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "create");

        // assert
        result.AssertError(
            """
            Missing required option '--name'.
            """);
    }

    [Fact]
    public async Task CreateApiThrows_ReturnsError()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupCreateApiMutationException("workspace-from-session", ApiName);

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "create",
            "--name",
            ApiName,
            "--path",
            "/products");

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating API 'my-api'
            └── ✕ Failed to create the API.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task CreateApiHasNoChanges_ReturnsError()
    {
        // arrange
        SetupCreateApiMutationNoChanges(WorkspaceId, ApiName);

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "create",
            "--workspace-id",
            WorkspaceId,
            "--name",
            ApiName,
            "--path",
            "/products");

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating API 'my-api'
            └── ✕ Failed to create the API.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the expected data.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task CreateApiHasChangeError_ReturnsError()
    {
        // arrange
        var changeError = new Mock<ICreateApiCommandMutation_PushWorkspaceChanges_Changes_Error>(MockBehavior.Strict);
        changeError
            .As<IError>()
            .SetupGet(x => x.Message)
            .Returns("Create denied");

        SetupCreateApiMutationWithChangeError(WorkspaceId, ApiName, changeError.Object);

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "create",
            "--workspace-id",
            WorkspaceId,
            "--name",
            ApiName,
            "--path",
            "/products");

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating API 'my-api'
            └── ✕ Failed to create the API.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Create denied
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task CreateApiHasMutationErrors_ReturnsError()
    {
        // arrange
        var mutationError = new Mock<ICreateApiCommandMutation_PushWorkspaceChanges_Errors>(MockBehavior.Strict);
        mutationError
            .As<IError>()
            .SetupGet(x => x.Message)
            .Returns("Mutation payload denied");

        SetupCreateApiMutationWithErrors(WorkspaceId, ApiName, mutationError.Object);

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "create",
            "--workspace-id",
            WorkspaceId,
            "--name",
            ApiName,
            "--path",
            "/products");

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating API 'my-api'
            └── ✕ Failed to create the API.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Unexpected mutation error: Mutation payload denied
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithOptions_ReturnsSuccess_NonInteractive()
    {
        // arrange
        SetupCreateApiMutation(
            WorkspaceId,
            ApiName,
            expectedPath: ["products", "catalog"],
            kind: ApiKind.Collection);

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "create",
            "--workspace-id",
            WorkspaceId,
            "--name",
            ApiName,
            "--path",
            "/products/catalog",
            "--kind",
            "collection");

        // assert
        result.AssertSuccess(
            """
            Creating API 'my-api'
            └── ✓ Created API 'my-api'.

            {
              "id": "api-1",
              "name": "my-api",
              "path": "products/catalog",
              "workspace": {
                "name": "Workspace"
              },
              "apiDetailPromptSettings": {
                "apiDetailPromptSchemaRegistry": {
                  "treatDangerousAsBreaking": true,
                  "allowBreakingSchemaChanges": false
                }
              }
            }
            """);
    }

    [Fact]
    public async Task WithOptions_ReturnsSuccess_OutputJson()
    {
        // arrange
        SetupInteractionMode(InteractionMode.JsonOutput);
        SetupCreateApiMutation(
            WorkspaceId,
            ApiName,
            expectedPath: ["products", "catalog"],
            kind: ApiKind.Collection);

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "create",
            "--workspace-id",
            WorkspaceId,
            "--name",
            ApiName,
            "--path",
            "/products/catalog",
            "--kind",
            "collection");

        // assert
        result.AssertSuccess(
            """
            {
              "id": "api-1",
              "name": "my-api",
              "path": "products/catalog",
              "workspace": {
                "name": "Workspace"
              },
              "apiDetailPromptSettings": {
                "apiDetailPromptSchemaRegistry": {
                  "treatDangerousAsBreaking": true,
                  "allowBreakingSchemaChanges": false
                }
              }
            }
            """);
    }

    [Fact]
    public async Task MissingRequiredOptions_PromptsUser_ReturnsSuccess()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);
        SetupCreateApiMutation(
            "workspace-from-session",
            ApiName,
            expectedPath: ["products"]);

        var command = StartInteractiveCommand(
            "api",
            "create");

        // act
        command.Input(ApiName);
        command.Input("/products");

        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task Create_Should_PromptForPath_When_NameProvidedButPathMissing_Interactive()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);
        SetupCreateApiMutation(
            "workspace-from-session",
            ApiName,
            expectedPath: ["products"]);

        var command = StartInteractiveCommand(
            "api",
            "create",
            "--name",
            ApiName);

        // act
        command.Input("/products");

        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task Create_Should_ReturnSuccess_When_KindIsService()
    {
        // arrange
        SetupCreateApiMutation(
            WorkspaceId,
            ApiName,
            expectedPath: ["products"],
            kind: ApiKind.Service);

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "create",
            "--workspace-id",
            WorkspaceId,
            "--name",
            ApiName,
            "--path",
            "/products",
            "--kind",
            "service");

        // assert
        result.AssertSuccess(
            """
            Creating API 'my-api'
            └── ✓ Created API 'my-api'.

            {
              "id": "api-1",
              "name": "my-api",
              "path": "products/catalog",
              "workspace": {
                "name": "Workspace"
              },
              "apiDetailPromptSettings": {
                "apiDetailPromptSchemaRegistry": {
                  "treatDangerousAsBreaking": true,
                  "allowBreakingSchemaChanges": false
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Create_Should_ReturnSuccess_When_KindIsGateway()
    {
        // arrange
        SetupCreateApiMutation(
            WorkspaceId,
            ApiName,
            expectedPath: ["products"],
            kind: ApiKind.Gateway);

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "create",
            "--workspace-id",
            WorkspaceId,
            "--name",
            ApiName,
            "--path",
            "/products",
            "--kind",
            "gateway");

        // assert
        result.AssertSuccess(
            """
            Creating API 'my-api'
            └── ✓ Created API 'my-api'.

            {
              "id": "api-1",
              "name": "my-api",
              "path": "products/catalog",
              "workspace": {
                "name": "Workspace"
              },
              "apiDetailPromptSettings": {
                "apiDetailPromptSchemaRegistry": {
                  "treatDangerousAsBreaking": true,
                  "allowBreakingSchemaChanges": false
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Create_Should_ReturnSuccess_When_KindNotProvided()
    {
        // arrange
        SetupCreateApiMutation(
            WorkspaceId,
            ApiName,
            expectedPath: ["products"]);

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "create",
            "--workspace-id",
            WorkspaceId,
            "--name",
            ApiName,
            "--path",
            "/products");

        // assert
        result.AssertSuccess(
            """
            Creating API 'my-api'
            └── ✓ Created API 'my-api'.

            {
              "id": "api-1",
              "name": "my-api",
              "path": "products/catalog",
              "workspace": {
                "name": "Workspace"
              },
              "apiDetailPromptSettings": {
                "apiDetailPromptSchemaRegistry": {
                  "treatDangerousAsBreaking": true,
                  "allowBreakingSchemaChanges": false
                }
              }
            }
            """);
    }
}
