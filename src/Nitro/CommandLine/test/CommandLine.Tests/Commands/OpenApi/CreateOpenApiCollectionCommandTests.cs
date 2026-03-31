using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.OpenApi;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.OpenApi;

public sealed class CreateOpenApiCollectionCommandTests
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "openapi",
                "create",
                "--help")
            .ExecuteAsync();

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
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddInteractionMode(mode)
            .AddArguments(
                "openapi",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "my-openapi")
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
    public async Task MissingRequiredName_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);

        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "openapi",
                "create",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Missing required option '--name'.
            """);

        apisClient.VerifyAll();
        openApiClient.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoWorkspaceInSession_And_NoApiId_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);

        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddSession()
            .AddInteractionMode(mode)
            .AddArguments(
                "openapi",
                "create",
                "--name",
                "my-openapi")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            You are not logged in. Run `[bold blue]nitro login[/]` to sign in or manually
            specify the '--workspace-id' option (if available).
            """);

        apisClient.VerifyAll();
        openApiClient.VerifyAll();
    }

    [Fact]
    public async Task WithOptions_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        openApiClient.Setup(x => x.CreateOpenApiCollectionAsync(
                "api-1",
                "my-openapi",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OpenApiCommandTestHelper.CreateOpenApiCollectionPayload("oa-1", "my-openapi"));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "openapi",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "my-openapi")
            .ExecuteAsync();

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

        apisClient.VerifyAll();
        openApiClient.VerifyAll();
    }

    [Fact]
    public async Task WithOptions_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        openApiClient.Setup(x => x.CreateOpenApiCollectionAsync(
                "api-1",
                "my-openapi",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OpenApiCommandTestHelper.CreateOpenApiCollectionPayload("oa-1", "my-openapi"));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "openapi",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "my-openapi")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "id": "oa-1",
              "name": "my-openapi"
            }
            """);

        apisClient.VerifyAll();
        openApiClient.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullResult_ReturnsError_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        openApiClient.Setup(x => x.CreateOpenApiCollectionAsync(
                "api-1",
                "my-openapi",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OpenApiCommandTestHelper.CreateOpenApiCollectionPayloadWithNullResult());

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "openapi",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "my-openapi")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating OpenAPI collection 'my-openapi' for API 'api-1'
            └── ✕ Failed to create the OpenAPI collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the
            expected data.
            """);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        openApiClient.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(CreateMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_NonInteractive(
        ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        openApiClient.Setup(x => x.CreateOpenApiCollectionAsync(
                "api-1",
                "my-openapi",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OpenApiCommandTestHelper.CreateOpenApiCollectionPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "openapi",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "my-openapi")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating OpenAPI collection 'my-openapi' for API 'api-1'
            └── ✕ Failed to create the OpenAPI collection.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        openApiClient.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(CreateMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_Interactive(
        ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        openApiClient.Setup(x => x.CreateOpenApiCollectionAsync(
                "api-1",
                "my-openapi",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OpenApiCommandTestHelper.CreateOpenApiCollectionPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "openapi",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "my-openapi")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Failed to create the OpenAPI collection.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        openApiClient.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(CreateMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_JsonOutput(
        ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        openApiClient.Setup(x => x.CreateOpenApiCollectionAsync(
                "api-1",
                "my-openapi",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OpenApiCommandTestHelper.CreateOpenApiCollectionPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "openapi",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "my-openapi")
            .ExecuteAsync();

        // assert
        result.AssertError(expectedStdErr);

        apisClient.VerifyAll();
        openApiClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        openApiClient.Setup(x => x.CreateOpenApiCollectionAsync(
                "api-1",
                "my-openapi",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "openapi",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "my-openapi")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating OpenAPI collection 'my-openapi' for API 'api-1'
            └── ✕ Failed to create the OpenAPI collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        openApiClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        openApiClient.Setup(x => x.CreateOpenApiCollectionAsync(
                "api-1",
                "my-openapi",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "openapi",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "my-openapi")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Failed to create the OpenAPI collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        openApiClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        openApiClient.Setup(x => x.CreateOpenApiCollectionAsync(
                "api-1",
                "my-openapi",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "openapi",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "my-openapi")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);

        apisClient.VerifyAll();
        openApiClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        openApiClient.Setup(x => x.CreateOpenApiCollectionAsync(
                "api-1",
                "my-openapi",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "openapi",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "my-openapi")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating OpenAPI collection 'my-openapi' for API 'api-1'
            └── ✕ Failed to create the OpenAPI collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        openApiClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        openApiClient.Setup(x => x.CreateOpenApiCollectionAsync(
                "api-1",
                "my-openapi",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "openapi",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "my-openapi")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Failed to create the OpenAPI collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        openApiClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        openApiClient.Setup(x => x.CreateOpenApiCollectionAsync(
                "api-1",
                "my-openapi",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "openapi",
                "create",
                "--api-id",
                "api-1",
                "--name",
                "my-openapi")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);

        apisClient.VerifyAll();
        openApiClient.VerifyAll();
    }

    public static TheoryData<ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection_Errors, string> CreateMutationErrorCases =>
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
