using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.ApiKeys;
using ChilliCream.Nitro.Client.Apis;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.ApiKeys;

public sealed class CreateApiKeyCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "api-key",
                "create",
                "--help")
            .ExecuteAsync();

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
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddInteractionMode(mode)
            .AddArguments(
                "api-key",
                "create",
                "--workspace-id",
                "ws-1",
                "--name",
                "key-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run
            'nitro login'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingRequiredOptions_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddApiKey()
            .AddSession()
            .AddInteractionMode(mode)
            .AddArguments(
                "api-key",
                "create")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Missing required option '--name'.
            """);
    }

    [Fact]
    public async Task MissingRequiredOptions_PromptsUser_SelectsApi_ReturnsSuccess()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        apisClient.Setup(x => x.SelectApisAsync(
                "workspace-from-session",
                null,
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionPage<ISelectApiPromptQuery_WorkspaceById_Apis_Edges_Node>(
                [
                    new SelectApiPromptQuery_WorkspaceById_Apis_Edges_Node_Api(
                        "api-1",
                        "Api 1",
                        [],
                        null,
                        new ShowApiCommandQuery_Node_Settings_ApiSettings(
                            new ShowApiCommandQuery_Node_Settings_SchemaRegistry_SchemaRegistrySettings(false, false)))
                ],
                null,
                false));
        var apiKeysClient = new Mock<IApiKeysClient>(MockBehavior.Strict);
        apiKeysClient.Setup(x => x.CreateApiKeyAsync(
                "integration",
                "workspace-from-session",
                "api-1",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                ApiKeyCommandTestHelper.CreateApiKeyResult("secret-123", "key-1", "integration", "Workspace"));

        var command = new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(apiKeysClient.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "api-key",
                "create")
            .Start();

        // act
        command.Input("integration"); // name
        command.SelectOption(0); // Api or Workspace
        command.SelectOption(0); // Api 1

        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccessful();
        apiKeysClient.VerifyAll();
    }

    [Fact]
    public async Task MissingRequiredOptions_PromptsUser_SelectsWorkspace_ReturnsSuccess()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "integration",
                "workspace-from-session",
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                ApiKeyCommandTestHelper.CreateApiKeyResult("secret-123", "key-1", "integration", "Workspace"));

        var command = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "api-key",
                "create")
            .Start();

        // act
        command.Input("integration"); // name
        command.SelectOption(1); // Api or Workspace

        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccessful();
        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingWorkspaceAndApi_ReturnsError(InteractionMode mode)
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        var builder = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddSession()
            .AddInteractionMode(mode)
            .AddArguments(
                "api-key",
                "create",
                "--name",
                "key-1");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.AssertError(
            """
            The '--workspace-id' or '--api-id' option is required in non-interactive mode.
            """);
        client.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithApiId_NoWorkspaceIdOption_NoWorkspaceInSession_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddSession()
            .AddInteractionMode(mode)
            .AddArguments(
                "api-key",
                "create",
                "--name",
                "key-1",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            You are not logged in. Run `[bold blue]nitro login[/]` to sign in or manually
            specify the '--workspace-id' option (if available).
            """);
    }

    [Fact]
    public async Task WithWorkspaceId_ReturnsSuccess_OutputJson()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "integration",
                "ws-1",
                null,
                "prod",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                ApiKeyCommandTestHelper.CreateApiKeyResult("secret-123", "key-1", "integration", "Workspace"));

        var builder = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "api-key",
                "create",
                "--workspace-id",
                "ws-1",
                "--name",
                "integration",
                "--stage-condition",
                "prod");

        // act
        var result = await builder.ExecuteAsync();

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

        client.VerifyAll();
    }

    [Fact]
    public async Task WithWorkspaceId_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "integration",
                "ws-1",
                null,
                "prod",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                ApiKeyCommandTestHelper.CreateApiKeyResult("secret-123", "key-1", "integration", "Workspace"));

        var builder = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api-key",
                "create",
                "--workspace-id",
                "ws-1",
                "--name",
                "integration",
                "--stage-condition",
                "prod");

        // act
        var result = await builder.ExecuteAsync();

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

        client.VerifyAll();
    }

    [Fact]
    public async Task WithApiId_WithWorkspaceIdFromSession_ReturnsSuccess_OutputJson()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "tenant-key",
                "workspace-from-session",
                "api-1",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiKeyCommandTestHelper.CreateApiKeyResult("secret-xyz", "key-9", "tenant-key", "Workspace"));

        var builder = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "api-key",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "tenant-key");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "secret": "secret-xyz",
              "details": {
                "id": "key-9",
                "name": "tenant-key",
                "workspace": {
                  "name": "Workspace"
                }
              }
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithApiId_WithWorkspaceId_ReturnsSuccess_OutputJson()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "tenant-key",
                "ws-1",
                "api-1",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                ApiKeyCommandTestHelper.CreateApiKeyResult("secret-xyz", "key-9", "tenant-key", "Workspace"));

        var builder = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddSession()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "api-key",
                "create",
                "--api-id",
                "api-1",
                "--workspace-id",
                "ws-1",
                "--name",
                "tenant-key");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "secret": "secret-xyz",
              "details": {
                "id": "key-9",
                "name": "tenant-key",
                "workspace": {
                  "name": "Workspace"
                }
              }
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithApiId_WithWorkspaceId_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "tenant-key",
                "ws-1",
                "api-1",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                ApiKeyCommandTestHelper.CreateApiKeyResult("secret-xyz", "key-9", "tenant-key", "Workspace"));

        var builder = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddSession()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api-key",
                "create",
                "--api-id",
                "api-1",
                "--workspace-id",
                "ws-1",
                "--name",
                "tenant-key");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Creating API key 'tenant-key'
            └── ✓ Created API key 'tenant-key'.

            {
              "secret": "secret-xyz",
              "details": {
                "id": "key-9",
                "name": "tenant-key",
                "workspace": {
                  "name": "Workspace"
                }
              }
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithApiId_WithWorkspaceIdFromSession_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "tenant-key",
                "workspace-from-session",
                "api-1",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiKeyCommandTestHelper.CreateApiKeyResult("secret-xyz", "key-9", "tenant-key", "Workspace"));

        var builder = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api-key",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "tenant-key");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Creating API key 'tenant-key'
            └── ✓ Created API key 'tenant-key'.

            {
              "secret": "secret-xyz",
              "details": {
                "id": "key-9",
                "name": "tenant-key",
                "workspace": {
                  "name": "Workspace"
                }
              }
            }
            """);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(CreateApiKeyMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_Interactive(
        ICreateApiKeyCommandMutation_CreateApiKey_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "tenant-key",
                "workspace-from-session",
                "api-404",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                ApiKeyCommandTestHelper.CreateApiKeyResultWithErrors(
                    mutationError));

        var builder = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "api-key",
                "create",
                "--api-id",
                "api-404",
                "--name",
                "tenant-key");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(CreateApiKeyMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_NonInteractive(
        ICreateApiKeyCommandMutation_CreateApiKey_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "tenant-key",
                "workspace-from-session",
                "api-404",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                ApiKeyCommandTestHelper.CreateApiKeyResultWithErrors(
                    mutationError));

        var builder = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api-key",
                "create",
                "--api-id",
                "api-404",
                "--name",
                "tenant-key");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating API key 'tenant-key'
            └── ✕ Failed to create the API key.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(CreateApiKeyMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_JsonOutput(
        ICreateApiKeyCommandMutation_CreateApiKey_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "tenant-key",
                "workspace-from-session",
                "api-404",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                ApiKeyCommandTestHelper.CreateApiKeyResultWithErrors(
                    mutationError));

        var builder = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "api-key",
                "create",
                "--api-id",
                "api-404",
                "--name",
                "tenant-key");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.AssertError(expectedStdErr);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_OutputJson()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "broken",
                "workspace-from-session",
                "api-1",
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        var builder = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddSessionWithWorkspace()
            .AddArguments(
                "api-key",
                "create",
                "--name",
                "broken",
                "--api-id",
                "api-1");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "broken",
                "workspace-from-session",
                "api-1",
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        var builder = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddSessionWithWorkspace()
            .AddArguments(
                "api-key",
                "create",
                "--name",
                "broken",
                "--api-id",
                "api-1");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating API key 'broken'
            └── ✕ Failed to create the API key.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "broken",
                "workspace-from-session",
                "api-1",
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        var builder = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddInteractionMode(InteractionMode.Interactive)
            .AddSessionWithWorkspace()
            .AddArguments(
                "api-key",
                "create",
                "--name",
                "broken",
                "--api-id",
                "api-1");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_OutputJson()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "broken",
                "workspace-from-session",
                "api-1",
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException());

        var builder = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddSessionWithWorkspace()
            .AddArguments(
                "api-key",
                "create",
                "--name",
                "broken",
                "--api-id",
                "api-1");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "broken",
                "workspace-from-session",
                "api-1",
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException());

        var builder = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddSessionWithWorkspace()
            .AddArguments(
                "api-key",
                "create",
                "--name",
                "broken",
                "--api-id",
                "api-1");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating API key 'broken'
            └── ✕ Failed to create the API key.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_Interactive()
    {
        // arrange
        var client = new Mock<IApiKeysClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiKeyAsync(
                "broken",
                "workspace-from-session",
                "api-1",
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException());

        var builder = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddInteractionMode(InteractionMode.Interactive)
            .AddSessionWithWorkspace()
            .AddArguments(
                "api-key",
                "create",
                "--name",
                "broken",
                "--api-id",
                "api-1");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    public static IEnumerable<object[]> CreateApiKeyMutationErrorCases()
    {
        yield return
        [
            new CreateApiKeyCommandMutation_CreateApiKey_Errors_ApiNotFoundError(
                "ApiNotFoundError",
                "The API with ID 'api-404' was not found.",
                "api-404"),
            """
            The API with ID 'api-404' was not found.
            """
        ];

        yield return
        [
            new CreateApiKeyCommandMutation_CreateApiKey_Errors_WorkspaceNotFound(
                "WorkspaceNotFound",
                "The workspace with ID 'ws-404' was not found.",
                "ws-404"),
            """
            The workspace with ID 'ws-404' was not found.
            """
        ];

        yield return
        [
            new CreateApiKeyCommandMutation_CreateApiKey_Errors_PersonalWorkspaceNotSupportedError(
                "PersonalWorkspaceNotSupportedError",
                "Personal workspaces are not supported for this operation."),
            """
            Personal workspaces are not supported for this operation.
            """
        ];

        yield return
        [
            new CreateApiKeyCommandMutation_CreateApiKey_Errors_RoleNotFoundError(
                "RoleNotFoundError",
                "The role with ID 'role-404' was not found.",
                "role-404"),
            """
            The role with ID 'role-404' was not found.
            """
        ];

        yield return
        [
            new CreateApiKeyCommandMutation_CreateApiKey_Errors_ValidationError(
                "ValidationError",
                "The input is invalid.",
                []),
            """
            The input is invalid.
            """
        ];

        var unexpectedError = new Mock<ICreateApiKeyCommandMutation_CreateApiKey_Errors>();
        unexpectedError
            .As<IError>()
            .SetupGet(x => x.Message)
            .Returns("unexpected failure");

        yield return
        [
            unexpectedError.Object,
            """
            Unexpected mutation error: unexpected failure
            """
        ];
    }
}
