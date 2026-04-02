using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Stages;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Stages;

public sealed class ListStagesCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "stage",
                "list",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List all stages of an API.

            Usage:
              nitro stage list [options]

            Options:
              --api-id <api-id>        The ID of the API [env: NITRO_API_ID]
              --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information

            Example:
              nitro stage list --api-id "<api-id>"
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
                "stage",
                "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingApiId_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var stagesClient = new Mock<IStagesClient>(MockBehavior.Strict);

        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(stagesClient.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "stage",
                "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The '--api-id' option is required in non-interactive mode.
            """);

        apisClient.VerifyAll();
        stagesClient.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithApiId_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var stagesClient = new Mock<IStagesClient>(MockBehavior.Strict);
        stagesClient.Setup(x => x.ListStagesAsync(
                "api-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListStagesResult(
                ("stage-1", "production", new[] { "staging" }),
                ("stage-2", "staging", Array.Empty<string>())));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(stagesClient.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "stage",
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
                  "id": "stage-1",
                  "name": "production",
                  "conditions": [
                    {
                      "kind": "AfterStage",
                      "name": "staging"
                    }
                  ]
                },
                {
                  "id": "stage-2",
                  "name": "staging",
                  "conditions": []
                }
              ],
              "cursor": null
            }
            """);

        apisClient.VerifyAll();
        stagesClient.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithApiId_NoData_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var stagesClient = new Mock<IStagesClient>(MockBehavior.Strict);
        stagesClient.Setup(x => x.ListStagesAsync(
                "api-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListStagesResult());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(stagesClient.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "stage",
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
        stagesClient.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var stagesClient = CreateListExceptionClient(
            new NitroClientGraphQLException("Some message.", "SOME_CODE"), "api-1");

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(stagesClient.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "stage",
                "list",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);

        apisClient.VerifyAll();
        stagesClient.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsAuthorizationException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var stagesClient = CreateListExceptionClient(
            new NitroClientAuthorizationException(), "api-1");

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(stagesClient.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "stage",
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
        stagesClient.VerifyAll();
    }

    [Fact]
    public async Task WithApiId_ReturnsSuccess_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var stagesClient = new Mock<IStagesClient>(MockBehavior.Strict);
        stagesClient.Setup(x => x.ListStagesAsync(
                "api-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListStagesResult(
                ("stage-1", "production", new[] { "staging" }),
                ("stage-2", "staging", Array.Empty<string>())));

        var command = new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(stagesClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "stage",
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
        stagesClient.VerifyAll();
    }

    [Fact]
    public async Task WithApiId_NoData_ReturnsSuccess_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var stagesClient = new Mock<IStagesClient>(MockBehavior.Strict);
        stagesClient.Setup(x => x.ListStagesAsync(
                "api-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListStagesResult());

        var command = new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(stagesClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "stage",
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
        stagesClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var stagesClient = CreateListExceptionClient(
            new NitroClientGraphQLException("Some message.", "SOME_CODE"), "api-1");

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(stagesClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "stage",
                "list",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        stagesClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var stagesClient = CreateListExceptionClient(
            new NitroClientAuthorizationException(), "api-1");

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(stagesClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "stage",
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
        stagesClient.VerifyAll();
    }

    private static IReadOnlyList<IListStagesQuery_Node_Stages> CreateListStagesResult(
        params (string Id, string Name, string[] AfterStageNames)[] stages)
    {
        return stages
            .Select(static s => CreateStage(s.Id, s.Name, s.AfterStageNames))
            .ToArray();
    }

    private static IListStagesQuery_Node_Stages CreateStage(
        string id,
        string name,
        string[] afterStageNames)
    {
        var conditions = afterStageNames
            .Select(static afterName =>
            {
                var afterStage = new Mock<IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Api_Stages_Conditions_AfterStage>(MockBehavior.Strict);
                afterStage.SetupGet(x => x.Name).Returns(afterName);

                var condition = new Mock<IListStagesQuery_Node_Stages_Conditions_AfterStageCondition>(MockBehavior.Strict);
                condition.SetupGet(x => x.AfterStage).Returns(afterStage.Object);

                return condition.As<IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Api_Stages_Conditions>().Object;
            })
            .ToArray();

        var stage = new Mock<IListStagesQuery_Node_Stages>(MockBehavior.Strict);
        stage.SetupGet(x => x.Id).Returns(id);
        stage.SetupGet(x => x.Name).Returns(name);
        stage.SetupGet(x => x.DisplayName).Returns(name);
        stage.SetupGet(x => x.Conditions).Returns(conditions);

        return stage.Object;
    }

    private static Mock<IStagesClient> CreateListExceptionClient(
        Exception ex,
        string apiId)
    {
        var client = new Mock<IStagesClient>(MockBehavior.Strict);
        client.Setup(x => x.ListStagesAsync(
                apiId,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }
}
