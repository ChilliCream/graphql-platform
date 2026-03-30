using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Exceptions;
using ChilliCream.Nitro.Client.OpenApi;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.OpenApi;

public sealed class ListOpenApiCollectionCommandTests
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "openapi",
                "list",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Lists all OpenAPI collections of an API

            Usage:
              nitro openapi list [options]

            Options:
                            --api-id <api-id>        The ID of the API [env: NITRO_API_ID]
                            --cursor <cursor>        The cursor to start the query (non interactive mode) [env: NITRO_CURSOR]
                            --cloud-url <cloud-url>  The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
                            --api-key <api-key>      The API key that is used for the authentication [env: NITRO_API_KEY]
                            --output <json>          The format in which the result should be displayed, if this option is set, the console will be non-interactive and the result will be displayed in the specified format [env: NITRO_OUTPUT_FORMAT]
                            -?, -h, --help           Show help and usage information
            """);
    }

    [Fact]
    public async Task NoApiKey_NoApiId_ReturnsError_Interactive()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "openapi",
                "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            You are not logged in. Run `[bold blue]nitro login[/]` to sign in or manually specify the '--workspace-id' option (if available).
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingApiId_ReturnsError(InteractionMode mode)
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
                "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The '--api-id' option is required in non-interactive mode.
            """);

        apisClient.VerifyAll();
        openApiClient.VerifyAll();
    }

    [Fact]
    public async Task NoWorkspaceInSession_And_NoApiId_ReturnsError_Interactive()
    {
        // arrange & act
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);

        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddSession()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "openapi",
                "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            You are not logged in. Run `[bold blue]nitro login[/]` to sign in or manually specify the '--workspace-id' option (if available).
            """);

        apisClient.VerifyAll();
        openApiClient.VerifyAll();
    }

    [Fact]
    public async Task WithApiId_ReturnsSuccess_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        openApiClient.Setup(x => x.ListOpenApiCollectionsAsync(
                "api-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OpenApiCommandTestHelper.CreateListPage(
                endCursor: null,
                hasNextPage: false,
                ("openapi-1", "auth-tools"),
                ("openapi-2", "data-tools")));

        var command = new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "openapi",
                "list",
                "--api-id",
                "api-1")
            .Start();

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        apisClient.VerifyAll();
        openApiClient.VerifyAll();
    }

    [Fact]
    public async Task WithApiId_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        openApiClient.Setup(x => x.ListOpenApiCollectionsAsync(
                "api-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OpenApiCommandTestHelper.CreateListPage(
                endCursor: null,
                hasNextPage: false,
                ("openapi-1", "auth-tools"),
                ("openapi-2", "data-tools")));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "openapi",
                "list",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """

            {
              "values": [
                {
                  "id": "openapi-1",
                  "name": "auth-tools"
                },
                {
                  "id": "openapi-2",
                  "name": "data-tools"
                }
              ],
              "cursor": null
            }
            """);

        apisClient.VerifyAll();
        openApiClient.VerifyAll();
    }

    [Fact]
    public async Task WithApiId_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        openApiClient.Setup(x => x.ListOpenApiCollectionsAsync(
                "api-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OpenApiCommandTestHelper.CreateListPage(
                endCursor: null,
                hasNextPage: false,
                ("openapi-1", "auth-tools"),
                ("openapi-2", "data-tools")));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "openapi",
                "list",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "id": "openapi-1",
                  "name": "auth-tools"
                },
                {
                  "id": "openapi-2",
                  "name": "data-tools"
                }
              ],
              "cursor": null
            }
            """);

        apisClient.VerifyAll();
        openApiClient.VerifyAll();
    }

    [Fact]
    public async Task WithApiId_NoData_ReturnsSuccess_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        openApiClient.Setup(x => x.ListOpenApiCollectionsAsync(
                "api-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OpenApiCommandTestHelper.CreateListPage());

        var command = new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "openapi",
                "list",
                "--api-id",
                "api-1")
            .Start();

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        apisClient.VerifyAll();
        openApiClient.VerifyAll();
    }

    [Fact]
    public async Task WithApiId_NoData_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        openApiClient.Setup(x => x.ListOpenApiCollectionsAsync(
                "api-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OpenApiCommandTestHelper.CreateListPage());

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "openapi",
                "list",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """

            {
              "values": [],
              "cursor": null
            }
            """);

        apisClient.VerifyAll();
        openApiClient.VerifyAll();
    }

    [Fact]
    public async Task WithApiId_NoData_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        openApiClient.Setup(x => x.ListOpenApiCollectionsAsync(
                "api-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OpenApiCommandTestHelper.CreateListPage());

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "openapi",
                "list",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "values": [],
              "cursor": null
            }
            """);

        apisClient.VerifyAll();
        openApiClient.VerifyAll();
    }

    [Fact]
    public async Task WithCursor_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        openApiClient.Setup(x => x.ListOpenApiCollectionsAsync(
                "api-1",
                "cursor-1",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OpenApiCommandTestHelper.CreateListPage(
                endCursor: null,
                hasNextPage: false,
                ("openapi-1", "auth-tools")));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "openapi",
                "list",
                "--api-id",
                "api-1",
                "--cursor",
                "cursor-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """

            {
              "values": [
                {
                  "id": "openapi-1",
                  "name": "auth-tools"
                }
              ],
              "cursor": null
            }
            """);

        apisClient.VerifyAll();
        openApiClient.VerifyAll();
    }

    [Fact]
    public async Task WithCursor_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        openApiClient.Setup(x => x.ListOpenApiCollectionsAsync(
                "api-1",
                "cursor-1",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OpenApiCommandTestHelper.CreateListPage(
                endCursor: null,
                hasNextPage: false,
                ("openapi-1", "auth-tools")));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "openapi",
                "list",
                "--api-id",
                "api-1",
                "--cursor",
                "cursor-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "id": "openapi-1",
                  "name": "auth-tools"
                }
              ],
              "cursor": null
            }
            """);

        apisClient.VerifyAll();
        openApiClient.VerifyAll();
    }

    [Fact]
    public async Task WithCursorPagination_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        openApiClient.Setup(x => x.ListOpenApiCollectionsAsync(
                "api-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OpenApiCommandTestHelper.CreateListPage(
                endCursor: "cursor-2",
                hasNextPage: true,
                ("openapi-1", "auth-tools"),
                ("openapi-2", "data-tools")));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "openapi",
                "list",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """

            {
              "values": [
                {
                  "id": "openapi-1",
                  "name": "auth-tools"
                },
                {
                  "id": "openapi-2",
                  "name": "data-tools"
                }
              ],
              "cursor": "cursor-2"
            }
            """);

        apisClient.VerifyAll();
        openApiClient.VerifyAll();
    }

    [Fact]
    public async Task WithCursorPagination_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var openApiClient = new Mock<IOpenApiClient>(MockBehavior.Strict);
        openApiClient.Setup(x => x.ListOpenApiCollectionsAsync(
                "api-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OpenApiCommandTestHelper.CreateListPage(
                endCursor: "cursor-2",
                hasNextPage: true,
                ("openapi-1", "auth-tools"),
                ("openapi-2", "data-tools")));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "openapi",
                "list",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "id": "openapi-1",
                  "name": "auth-tools"
                },
                {
                  "id": "openapi-2",
                  "name": "data-tools"
                }
              ],
              "cursor": "cursor-2"
            }
            """);

        apisClient.VerifyAll();
        openApiClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var openApiClient = CreateListExceptionClient(new NitroClientException("list failed"), "api-1", null);

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "openapi",
                "list",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            There was an unexpected error executing your request: list failed
            """);

        apisClient.VerifyAll();
        openApiClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var openApiClient = CreateListExceptionClient(new NitroClientException("list failed"), "api-1", null);

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "openapi",
                "list",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error executing your request: list failed
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
        var openApiClient = CreateListExceptionClient(new NitroClientException("list failed"), "api-1", null);

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "openapi",
                "list",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            There was an unexpected error executing your request: list failed
            """);

        apisClient.VerifyAll();
        openApiClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var openApiClient = CreateListExceptionClient(new NitroClientAuthorizationException("forbidden"), "api-1", null);

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "openapi",
                "list",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);

        apisClient.VerifyAll();
        openApiClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var openApiClient = CreateListExceptionClient(new NitroClientAuthorizationException("forbidden"), "api-1", null);

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "openapi",
                "list",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
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
        var openApiClient = CreateListExceptionClient(new NitroClientAuthorizationException("forbidden"), "api-1", null);

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(openApiClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "openapi",
                "list",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);

        apisClient.VerifyAll();
        openApiClient.VerifyAll();
    }

    private static Mock<IOpenApiClient> CreateListExceptionClient(
        Exception ex,
        string apiId,
        string? cursor)
    {
        var client = new Mock<IOpenApiClient>(MockBehavior.Strict);
        client.Setup(x => x.ListOpenApiCollectionsAsync(
                apiId,
                cursor,
                10,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }
}
