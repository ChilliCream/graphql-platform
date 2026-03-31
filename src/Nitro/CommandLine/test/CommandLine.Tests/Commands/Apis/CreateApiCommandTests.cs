using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Apis;

public sealed class CreateApiCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "api",
                "create",
                "--help")
            .ExecuteAsync();

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
                "api",
                "create",
                "--workspace-id",
                "ws-1",
                "--name",
                "my-api",
                "--path",
                "/products")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run
            'nitro login'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoWorkspaceInSession_And_NoWorkspaceOption_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddApiKey()
            .AddSession()
            .AddInteractionMode(mode)
            .AddArguments(
                "api",
                "create",
                "--name",
                "my-api",
                "--path",
                "/products")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            You are not logged in. Run `[bold blue]nitro login[/]` to sign in or manually
            specify the '--workspace-id' option (if available).
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
            .AddSessionWithWorkspace()
            .AddInteractionMode(mode)
            .AddArguments(
                "api",
                "create")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Missing required option '--name'.
            """);
    }

    [Fact]
    public async Task WithOptions_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiAsync(
                "ws-1",
                It.Is<IReadOnlyList<string>>(p => p.SequenceEqual(new[] { "products", "catalog" })),
                "my-api",
                ApiKind.Collection,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateApiSuccessPayload());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api",
                "create",
                "--workspace-id",
                "ws-1",
                "--name",
                "my-api",
                "--path",
                "/products/catalog",
                "--kind",
                "collection")
            .ExecuteAsync();

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

        client.VerifyAll();
    }

    [Fact]
    public async Task WithOptions_ReturnsSuccess_OutputJson()
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiAsync(
                "ws-1",
                It.Is<IReadOnlyList<string>>(p => p.SequenceEqual(new[] { "products", "catalog" })),
                "my-api",
                ApiKind.Collection,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateApiSuccessPayload());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "api",
                "create",
                "--workspace-id",
                "ws-1",
                "--name",
                "my-api",
                "--path",
                "/products/catalog",
                "--kind",
                "collection")
            .ExecuteAsync();

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

        client.VerifyAll();
    }

    [Fact]
    public async Task MissingRequiredOptions_PromptsUser_ReturnsSuccess()
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiAsync(
                "workspace-from-session",
                It.Is<IReadOnlyList<string>>(p => p.SequenceEqual(new[] { "products" })),
                "my-api",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateApiSuccessPayload());

        var command = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "api",
                "create")
            .Start();

        // act
        command.Input("my-api");
        command.Input("/products");

        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNoChangeResult_ReturnsError()
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiAsync(
                "ws-1",
                It.IsAny<IReadOnlyList<string>>(),
                "my-api",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateApiPayloadWithNoChanges());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api",
                "create",
                "--workspace-id",
                "ws-1",
                "--name",
                "my-api",
                "--path",
                "/products")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating API 'my-api'
            └── ✕ Failed to create the API.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the
            expected data.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsChangeError_ReturnsError_NonInteractive()
    {
        // arrange
        var changeError = new Mock<ICreateApiCommandMutation_PushWorkspaceChanges_Changes_Error>(MockBehavior.Strict);
        changeError
            .As<IError>()
            .SetupGet(x => x.Message)
            .Returns("Create denied");

        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiAsync(
                "ws-1",
                It.IsAny<IReadOnlyList<string>>(),
                "my-api",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateApiPayloadWithChangeError(changeError.Object));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api",
                "create",
                "--workspace-id",
                "ws-1",
                "--name",
                "my-api",
                "--path",
                "/products")
            .ExecuteAsync();

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

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MutationReturnsChangeError_ReturnsError(InteractionMode mode)
    {
        // arrange
        var changeError = new Mock<ICreateApiCommandMutation_PushWorkspaceChanges_Changes_Error>(MockBehavior.Strict);
        changeError
            .As<IError>()
            .SetupGet(x => x.Message)
            .Returns("Create denied");

        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiAsync(
                "workspace-from-session",
                It.IsAny<IReadOnlyList<string>>(),
                "my-api",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateApiPayloadWithChangeError(changeError.Object));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddSessionWithWorkspace()
            .AddInteractionMode(mode)
            .AddArguments(
                "api",
                "create",
                "--name",
                "my-api",
                "--path",
                "/products")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Create denied
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsError_ReturnsError_NonInteractive()
    {
        // arrange
        var mutationError = new Mock<ICreateApiCommandMutation_PushWorkspaceChanges_Errors>(MockBehavior.Strict);
        mutationError
            .As<IError>()
            .SetupGet(x => x.Message)
            .Returns("Mutation payload denied");

        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiAsync(
                "ws-1",
                It.IsAny<IReadOnlyList<string>>(),
                "my-api",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateApiPayloadWithMutationErrors(mutationError.Object));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api",
                "create",
                "--workspace-id",
                "ws-1",
                "--name",
                "my-api",
                "--path",
                "/products")
            .ExecuteAsync();

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

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MutationReturnsError_ReturnsError(InteractionMode mode)
    {
        // arrange
        var mutationError = new Mock<ICreateApiCommandMutation_PushWorkspaceChanges_Errors>(MockBehavior.Strict);
        mutationError
            .As<IError>()
            .SetupGet(x => x.Message)
            .Returns("Mutation payload denied");

        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiAsync(
                "workspace-from-session",
                It.IsAny<IReadOnlyList<string>>(),
                "my-api",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateApiPayloadWithMutationErrors(mutationError.Object));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddSessionWithWorkspace()
            .AddInteractionMode(mode)
            .AddArguments(
                "api",
                "create",
                "--name",
                "my-api",
                "--path",
                "/products")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Unexpected mutation error: Mutation payload denied
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiAsync(
                "workspace-from-session",
                It.IsAny<IReadOnlyList<string>>(),
                "my-api",
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(mode)
            .AddArguments(
                "api",
                "create",
                "--name",
                "my-api",
                "--path",
                "/products")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiAsync(
                "workspace-from-session",
                It.IsAny<IReadOnlyList<string>>(),
                "my-api",
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api",
                "create",
                "--name",
                "my-api",
                "--path",
                "/products")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating API 'my-api'
            └── ✕ Failed to create the API.
            """);

        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsAuthorizationException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiAsync(
                "workspace-from-session",
                It.IsAny<IReadOnlyList<string>>(),
                "my-api",
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(mode)
            .AddArguments(
                "api",
                "create",
                "--name",
                "my-api",
                "--path",
                "/products")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiAsync(
                "workspace-from-session",
                It.IsAny<IReadOnlyList<string>>(),
                "my-api",
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api",
                "create",
                "--name",
                "my-api",
                "--path",
                "/products")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating API 'my-api'
            └── ✕ Failed to create the API.
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
    public async Task Create_Should_PromptForPath_When_NameProvidedButPathMissing_Interactive()
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiAsync(
                "workspace-from-session",
                It.Is<IReadOnlyList<string>>(p => p.SequenceEqual(new[] { "products" })),
                "my-api",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateApiSuccessPayload());

        var command = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "api",
                "create",
                "--name",
                "my-api")
            .Start();

        // act
        command.Input("/products");

        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();

        client.VerifyAll();
    }

    [Fact]
    public async Task Create_Should_ReturnSuccess_When_KindIsService()
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiAsync(
                "ws-1",
                It.Is<IReadOnlyList<string>>(p => p.SequenceEqual(new[] { "products" })),
                "my-api",
                ApiKind.Service,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateApiSuccessPayload());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api",
                "create",
                "--workspace-id",
                "ws-1",
                "--name",
                "my-api",
                "--path",
                "/products",
                "--kind",
                "service")
            .ExecuteAsync();

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

        client.VerifyAll();
    }

    [Fact]
    public async Task Create_Should_ReturnSuccess_When_KindIsGateway()
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiAsync(
                "ws-1",
                It.Is<IReadOnlyList<string>>(p => p.SequenceEqual(new[] { "products" })),
                "my-api",
                ApiKind.Gateway,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateApiSuccessPayload());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api",
                "create",
                "--workspace-id",
                "ws-1",
                "--name",
                "my-api",
                "--path",
                "/products",
                "--kind",
                "gateway")
            .ExecuteAsync();

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

        client.VerifyAll();
    }

    [Fact]
    public async Task Create_Should_ReturnSuccess_When_KindNotProvided()
    {
        // arrange
        var client = new Mock<IApisClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateApiAsync(
                "ws-1",
                It.Is<IReadOnlyList<string>>(p => p.SequenceEqual(new[] { "products" })),
                "my-api",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateApiSuccessPayload());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "api",
                "create",
                "--workspace-id",
                "ws-1",
                "--name",
                "my-api",
                "--path",
                "/products")
            .ExecuteAsync();

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

        client.VerifyAll();
    }

    private static ICreateApiCommandMutation_PushWorkspaceChanges CreateApiSuccessPayload()
    {
        var change = new Mock<ICreateApiCommandMutation_PushWorkspaceChanges_Changes>(MockBehavior.Strict);
        change.SetupGet(x => x.Error).Returns((ICreateApiCommandMutation_PushWorkspaceChanges_Changes_Error?)null);

        var settings = new ShowApiCommandQuery_Node_Settings_ApiSettings(
            new ShowApiCommandQuery_Node_Settings_SchemaRegistry_SchemaRegistrySettings(
                true,
                false));

        var workspace = new Mock<IShowApiCommandQuery_Node_Workspace_1>(MockBehavior.Strict);
        workspace.SetupGet(x => x.Name).Returns("Workspace");

        var result = new Mock<ICreateApiCommandMutation_PushWorkspaceChanges_Changes_Result_Api>(MockBehavior.Strict);
        result.SetupGet(x => x.Id).Returns("api-1");
        result.SetupGet(x => x.Name).Returns("my-api");
        result.SetupGet(x => x.Path).Returns(["products", "catalog"]);
        result.SetupGet(x => x.Workspace).Returns(workspace.Object);
        result.SetupGet(x => x.Settings).Returns(settings);

        change.SetupGet(x => x.Result).Returns(result.Object);

        var payload = new Mock<ICreateApiCommandMutation_PushWorkspaceChanges>(MockBehavior.Strict);
        payload.SetupGet(x => x.Changes).Returns([change.Object]);
        payload.SetupGet(x => x.Errors).Returns(Array.Empty<ICreateApiCommandMutation_PushWorkspaceChanges_Errors>());

        return payload.Object;
    }

    private static ICreateApiCommandMutation_PushWorkspaceChanges CreateApiPayloadWithNoChanges()
    {
        var payload = new Mock<ICreateApiCommandMutation_PushWorkspaceChanges>(MockBehavior.Strict);
        payload.SetupGet(x => x.Changes).Returns(Array.Empty<ICreateApiCommandMutation_PushWorkspaceChanges_Changes>());
        payload.SetupGet(x => x.Errors).Returns(Array.Empty<ICreateApiCommandMutation_PushWorkspaceChanges_Errors>());

        return payload.Object;
    }

    private static ICreateApiCommandMutation_PushWorkspaceChanges CreateApiPayloadWithChangeError(
        ICreateApiCommandMutation_PushWorkspaceChanges_Changes_Error error)
    {
        var change = new Mock<ICreateApiCommandMutation_PushWorkspaceChanges_Changes>(MockBehavior.Strict);
        change.SetupGet(x => x.Error).Returns(error);
        change.SetupGet(x => x.Result).Returns((ICreateApiCommandMutation_PushWorkspaceChanges_Changes_Result?)null);

        var payload = new Mock<ICreateApiCommandMutation_PushWorkspaceChanges>(MockBehavior.Strict);
        payload.SetupGet(x => x.Changes).Returns([change.Object]);
        payload.SetupGet(x => x.Errors).Returns(Array.Empty<ICreateApiCommandMutation_PushWorkspaceChanges_Errors>());

        return payload.Object;
    }

    private static ICreateApiCommandMutation_PushWorkspaceChanges CreateApiPayloadWithResult(
        ICreateApiCommandMutation_PushWorkspaceChanges_Changes_Result result)
    {
        var change = new Mock<ICreateApiCommandMutation_PushWorkspaceChanges_Changes>(MockBehavior.Strict);
        change.SetupGet(x => x.Error).Returns((ICreateApiCommandMutation_PushWorkspaceChanges_Changes_Error?)null);
        change.SetupGet(x => x.Result).Returns(result);

        var payload = new Mock<ICreateApiCommandMutation_PushWorkspaceChanges>(MockBehavior.Strict);
        payload.SetupGet(x => x.Changes).Returns([change.Object]);
        payload.SetupGet(x => x.Errors).Returns(Array.Empty<ICreateApiCommandMutation_PushWorkspaceChanges_Errors>());

        return payload.Object;
    }

    private static ICreateApiCommandMutation_PushWorkspaceChanges CreateApiPayloadWithMutationErrors(
        params ICreateApiCommandMutation_PushWorkspaceChanges_Errors[] errors)
    {
        var payload = new Mock<ICreateApiCommandMutation_PushWorkspaceChanges>(MockBehavior.Strict);
        payload.SetupGet(x => x.Changes).Returns(Array.Empty<ICreateApiCommandMutation_PushWorkspaceChanges_Changes>());
        payload.SetupGet(x => x.Errors).Returns(errors);

        return payload.Object;
    }
}
