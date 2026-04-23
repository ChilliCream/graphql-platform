using ChilliCream.Nitro.Client;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.ApiKeys;

public sealed class CreateApiKeyCommandTests(NitroCommandFixture fixture) : ApiKeysCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "api-key",
            "create",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Create a new API key.

            Usage:
              nitro api-key create [options]

            Options:
              --name <name>                        The name of the API key (for later reference) [env: NITRO_API_KEY_NAME]
              --api-id <api-id>                    The ID of the API [env: NITRO_API_ID]
              --workspace-id <workspace-id>        The ID of the workspace [env: NITRO_WORKSPACE_ID]
              --stage-condition <stage-condition>  [Preview] Limit the API key to a specific stage name (if not set, the key is valid for all stages)
              --cloud-url <cloud-url>              The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                  The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                      The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                       Show help and usage information

            Example:
              nitro api-key create \
                --name "my-api-key" \
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
            "api-key",
            "create",
            "--workspace-id",
            WorkspaceId,
            "--name",
            ApiKeyName);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run `nitro login`.
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
            "api-key",
            "create");

        // assert
        result.AssertError(
            """
            Missing required option '--name'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingWorkspaceAndApi_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "api-key",
            "create",
            "--name",
            ApiKeyName);

        // assert
        result.AssertError(
            """
            Missing required option '--workspace-id' or '--api-id'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithApiId_NoWorkspaceIdOption_NoWorkspaceInSession_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupSession();
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "api-key",
            "create",
            "--name",
            ApiKeyName,
            "--api-id",
            ApiId);

        // assert
        result.AssertError(
            """
            Could not determine workspace. Either login via `nitro login` or specify the '--workspace-id' option.
            """);
    }

    [Fact]
    public async Task CreateApiKeyThrows_ReturnsError()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupCreateApiKeyMutationException("broken", "workspace-from-session", apiId: ApiId);

        // act
        var result = await ExecuteCommandAsync(
            "api-key",
            "create",
            "--name",
            "broken",
            "--api-id",
            ApiId);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Creating API key 'broken'
            └── ✕ Failed to create the API key.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetCreateApiKeyErrors))]
    public async Task CreateApiKeyHasErrors_ReturnsError(
        ICreateApiKeyCommandMutation_CreateApiKey_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupCreateApiKeyMutation(
            "tenant-key",
            "workspace-from-session",
            apiId: "api-404",
            errors: error);

        // act
        var result = await ExecuteCommandAsync(
            "api-key",
            "create",
            "--api-id",
            "api-404",
            "--name",
            "tenant-key");

        // assert
        result.StdErr.MatchInlineSnapshot(expectedErrorMessage);
        result.StdOut.MatchInlineSnapshot(
            """
            Creating API key 'tenant-key'
            └── ✕ Failed to create the API key.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task CreateApiKeyReturnsNullResult_ReturnsError()
    {
        // arrange
        SetupCreateApiKeyMutationNullResult(ApiKeyName, WorkspaceId);

        // act
        var result = await ExecuteCommandAsync(
            "api-key",
            "create",
            "--workspace-id",
            WorkspaceId,
            "--name",
            ApiKeyName);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating API key 'my-key'
            └── ✕ Failed to create the API key.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the expected data.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task MissingRequiredOptions_PromptsUser_SelectsApi_ReturnsSuccess()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);
        SetupSelectApisPrompt((ApiId, "Api 1"));
        SetupCreateApiKeyMutation("integration", "workspace-from-session", apiId: ApiId);

        var command = StartInteractiveCommand(
            "api-key",
            "create");

        // act
        command.Input("integration"); // name
        command.SelectOption(0); // Api or Workspace
        command.SelectOption(0); // Api 1

        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task MissingRequiredOptions_PromptsUser_SelectsWorkspace_ReturnsSuccess()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);
        SetupCreateApiKeyMutation("integration", "workspace-from-session");

        var command = StartInteractiveCommand(
            "api-key",
            "create");

        // act
        command.Input("integration"); // name
        command.SelectOption(1); // Api or Workspace

        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task MissingName_PromptsUser_ReturnsSuccess()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);
        SetupCreateApiKeyMutation(ApiKeyName, "workspace-from-session", apiId: ApiId);

        var command = StartInteractiveCommand(
            "api-key",
            "create",
            "--api-id",
            ApiId);

        // act
        command.Input(ApiKeyName);

        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task WithWorkspaceId_ReturnsSuccess_OutputJson()
    {
        // arrange
        SetupCreateApiKeyMutation("integration", WorkspaceId, stageCondition: "prod");

        // act
        var result = await ExecuteCommandAsync(
            "api-key",
            "create",
            "--workspace-id",
            WorkspaceId,
            "--name",
            "integration",
            "--stage-condition",
            "prod",
            "--output",
            "json");

        // assert
        result.AssertSuccess(
            """
            {
              "secret": "secret-123",
              "details": {
                "id": "key-1",
                "name": "integration",
                "workspace": {
                  "name": "Workspace"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task WithWorkspaceId_ReturnsSuccess_NonInteractive()
    {
        // arrange
        SetupCreateApiKeyMutation("integration", WorkspaceId, stageCondition: "prod");

        // act
        var result = await ExecuteCommandAsync(
            "api-key",
            "create",
            "--workspace-id",
            WorkspaceId,
            "--name",
            "integration",
            "--stage-condition",
            "prod");

        // assert
        result.AssertSuccess(
            """
            Creating API key 'integration'
            └── ✓ Created API key 'integration'.

            {
              "secret": "secret-123",
              "details": {
                "id": "key-1",
                "name": "integration",
                "workspace": {
                  "name": "Workspace"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task WithApiId_WithWorkspaceIdFromSession_ReturnsSuccess_OutputJson()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupCreateApiKeyMutation("tenant-key", "workspace-from-session", apiId: ApiId);

        // act
        var result = await ExecuteCommandAsync(
            "api-key",
            "create",
            "--api-id",
            ApiId,
            "--name",
            "tenant-key",
            "--output",
            "json");

        // assert
        result.AssertSuccess(
            """
            {
              "secret": "secret-123",
              "details": {
                "id": "key-1",
                "name": "tenant-key",
                "workspace": {
                  "name": "Workspace"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task WithApiId_WithWorkspaceId_ReturnsSuccess_OutputJson()
    {
        // arrange
        SetupSession();
        SetupCreateApiKeyMutation("tenant-key", WorkspaceId, apiId: ApiId);

        // act
        var result = await ExecuteCommandAsync(
            "api-key",
            "create",
            "--api-id",
            ApiId,
            "--workspace-id",
            WorkspaceId,
            "--name",
            "tenant-key",
            "--output",
            "json");

        // assert
        result.AssertSuccess(
            """
            {
              "secret": "secret-123",
              "details": {
                "id": "key-1",
                "name": "tenant-key",
                "workspace": {
                  "name": "Workspace"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task WithApiId_WithWorkspaceId_ReturnsSuccess_NonInteractive()
    {
        // arrange
        SetupSession();
        SetupCreateApiKeyMutation("tenant-key", WorkspaceId, apiId: ApiId);

        // act
        var result = await ExecuteCommandAsync(
            "api-key",
            "create",
            "--api-id",
            ApiId,
            "--workspace-id",
            WorkspaceId,
            "--name",
            "tenant-key");

        // assert
        result.AssertSuccess(
            """
            Creating API key 'tenant-key'
            └── ✓ Created API key 'tenant-key'.

            {
              "secret": "secret-123",
              "details": {
                "id": "key-1",
                "name": "tenant-key",
                "workspace": {
                  "name": "Workspace"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task WithApiId_WithWorkspaceIdFromSession_ReturnsSuccess_NonInteractive()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupCreateApiKeyMutation("tenant-key", "workspace-from-session", apiId: ApiId);

        // act
        var result = await ExecuteCommandAsync(
            "api-key",
            "create",
            "--api-id",
            ApiId,
            "--name",
            "tenant-key");

        // assert
        result.AssertSuccess(
            """
            Creating API key 'tenant-key'
            └── ✓ Created API key 'tenant-key'.

            {
              "secret": "secret-123",
              "details": {
                "id": "key-1",
                "name": "tenant-key",
                "workspace": {
                  "name": "Workspace"
                }
              }
            }
            """);
    }

    public static TheoryData<ICreateApiKeyCommandMutation_CreateApiKey_Errors, string>
        GetCreateApiKeyErrors()
    {
        var unexpectedError = new Mock<ICreateApiKeyCommandMutation_CreateApiKey_Errors>();
        unexpectedError
            .As<IError>()
            .SetupGet(x => x.Message)
            .Returns("unexpected failure");

        return new()
        {
            {
                new CreateApiKeyCommandMutation_CreateApiKey_Errors_ApiNotFoundError(
                    "ApiNotFoundError",
                    "The API with ID 'api-404' was not found.",
                    "api-404"),
                "The API with ID 'api-404' was not found."
            },
            {
                new CreateApiKeyCommandMutation_CreateApiKey_Errors_WorkspaceNotFound(
                    "WorkspaceNotFound",
                    "The workspace with ID 'ws-404' was not found.",
                    "ws-404"),
                "The workspace with ID 'ws-404' was not found."
            },
            {
                new CreateApiKeyCommandMutation_CreateApiKey_Errors_PersonalWorkspaceNotSupportedError(
                    "PersonalWorkspaceNotSupportedError",
                    "Personal workspaces are not supported for this operation."),
                "Personal workspaces are not supported for this operation."
            },
            {
                new CreateApiKeyCommandMutation_CreateApiKey_Errors_RoleNotFoundError(
                    "RoleNotFoundError",
                    "The role with ID 'role-404' was not found.",
                    "role-404"),
                "The role with ID 'role-404' was not found."
            },
            {
                new CreateApiKeyCommandMutation_CreateApiKey_Errors_ValidationError(
                    "ValidationError",
                    "The input is invalid.",
                    []),
                "The input is invalid."
            },
            {
                unexpectedError.Object,
                "Unexpected mutation error: unexpected failure"
            }
        };
    }
}
