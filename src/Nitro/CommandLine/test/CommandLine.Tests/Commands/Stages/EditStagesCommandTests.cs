using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Stages;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Stages;

public sealed class EditStagesCommandTests
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "stage",
                "edit",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Edit stages of an API.

            Usage:
              nitro stage edit [options]

            Options:
              --api-id <api-id>                The ID of the API [env: NITRO_API_ID]
              --configuration <configuration>  The stage configuration. If not provided, an interactive selection will beshown. This input is a JSON array of stage configuration in the following format:[{"name":"stage1","displayName":"Stage 1","conditions":[{"afterStage":"stage2"}]},...]
              --cloud-url <cloud-url>          The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>              The API key that is used for the authentication [env: NITRO_API_KEY]
              --output <json>                  The format in which the result should be displayed, if this option is set, the console will be non-interactive and the result will be displayed in the specified format [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                   Show help and usage information
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
                "stage",
                "edit",
                "--api-id",
                "api-1",
                "--configuration",
                """[{"name":"dev","displayName":"Dev","conditions":[]}]""")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run
            'nitro login'.
            """);
    }

    [Fact]
    public async Task WithJsonConfig_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var stagesClient = new Mock<IStagesClient>(MockBehavior.Strict);
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);

        stagesClient.Setup(x => x.UpdateStagesAsync(
                "api-1",
                It.Is<IReadOnlyList<StageUpdateModel>>(s =>
                    s.Count == 1
                    && s[0].Name == "dev"
                    && s[0].DisplayName == "Dev"
                    && s[0].AfterStages.Count == 0),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUpdateStagesSuccessPayload());

        // act
        var result = await new CommandBuilder()
            .AddService(stagesClient.Object)
            .AddService(apisClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "stage",
                "edit",
                "--api-id",
                "api-1",
                "--configuration",
                """[{"name":"dev","displayName":"Dev","conditions":[]}]""")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            Update stages

            ? For which API do you want to edit the stages?: api-1
            Updating stages for API 'api-1'
            └── ✓ Updated stages for API 'api-1'.

            {
              "values": [
                {
                  "id": "stage-1",
                  "name": "dev",
                  "conditions": []
                }
              ],
              "cursor": null
            }
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        stagesClient.VerifyAll();
        apisClient.VerifyAll();
    }

    [Fact]
    public async Task WithJsonConfig_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var stagesClient = new Mock<IStagesClient>(MockBehavior.Strict);
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);

        stagesClient.Setup(x => x.UpdateStagesAsync(
                "api-1",
                It.Is<IReadOnlyList<StageUpdateModel>>(s =>
                    s.Count == 1
                    && s[0].Name == "dev"
                    && s[0].DisplayName == "Dev"
                    && s[0].AfterStages.Count == 0),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUpdateStagesSuccessPayload());

        // act
        var result = await new CommandBuilder()
            .AddService(stagesClient.Object)
            .AddService(apisClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "stage",
                "edit",
                "--api-id",
                "api-1",
                "--configuration",
                """[{"name":"dev","displayName":"Dev","conditions":[]}]""")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            {
              "values": [
                {
                  "id": "stage-1",
                  "name": "dev",
                  "conditions": []
                }
              ],
              "cursor": null
            }
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        stagesClient.VerifyAll();
        apisClient.VerifyAll();
    }

    [Fact]
    public async Task WithJsonConfig_WithConditions_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var stagesClient = new Mock<IStagesClient>(MockBehavior.Strict);
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);

        stagesClient.Setup(x => x.UpdateStagesAsync(
                "api-1",
                It.Is<IReadOnlyList<StageUpdateModel>>(s =>
                    s.Count == 2
                    && s[0].Name == "dev"
                    && s[1].Name == "prod"
                    && s[1].AfterStages.Count == 1
                    && s[1].AfterStages[0] == "dev"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUpdateStagesSuccessPayload());

        // act
        var result = await new CommandBuilder()
            .AddService(stagesClient.Object)
            .AddService(apisClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "stage",
                "edit",
                "--api-id",
                "api-1",
                "--configuration",
                """[{"name":"dev","displayName":"Dev","conditions":[]},{"name":"prod","displayName":"Production","conditions":[{"afterStage":"dev"}]}]""")
            .ExecuteAsync();

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        stagesClient.VerifyAll();
        apisClient.VerifyAll();
    }

    [Fact]
    public async Task WithInvalidJsonConfig_ReturnsError_NonInteractive()
    {
        // arrange
        var stagesClient = new Mock<IStagesClient>(MockBehavior.Strict);
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);

        // act
        var result = await new CommandBuilder()
            .AddService(stagesClient.Object)
            .AddService(apisClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "stage",
                "edit",
                "--api-id",
                "api-1",
                "--configuration",
                "not-valid-json")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Could not parse stage configuration
            """);
        Assert.Equal(1, result.ExitCode);

        stagesClient.VerifyAll();
        apisClient.VerifyAll();
    }

    [Fact]
    public async Task WithInvalidJsonConfig_ReturnsError_JsonOutput()
    {
        // arrange
        var stagesClient = new Mock<IStagesClient>(MockBehavior.Strict);
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);

        // act
        var result = await new CommandBuilder()
            .AddService(stagesClient.Object)
            .AddService(apisClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "stage",
                "edit",
                "--api-id",
                "api-1",
                "--configuration",
                "not-valid-json")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Could not parse stage configuration
            """);

        stagesClient.VerifyAll();
        apisClient.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(UpdateStagesMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_NonInteractive(
        IUpdateStages_UpdateStages_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var stagesClient = new Mock<IStagesClient>(MockBehavior.Strict);
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);

        stagesClient.Setup(x => x.UpdateStagesAsync(
                "api-1",
                It.IsAny<IReadOnlyList<StageUpdateModel>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUpdateStagesPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder()
            .AddService(stagesClient.Object)
            .AddService(apisClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "stage",
                "edit",
                "--api-id",
                "api-1",
                "--configuration",
                """[{"name":"dev","displayName":"Dev","conditions":[]}]""")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            Update stages

            ? For which API do you want to edit the stages?: api-1
            Updating stages for API 'api-1'
            └── ✕ Failed to update the stages.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        stagesClient.VerifyAll();
        apisClient.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(UpdateStagesMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_JsonOutput(
        IUpdateStages_UpdateStages_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var stagesClient = new Mock<IStagesClient>(MockBehavior.Strict);
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);

        stagesClient.Setup(x => x.UpdateStagesAsync(
                "api-1",
                It.IsAny<IReadOnlyList<StageUpdateModel>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUpdateStagesPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder()
            .AddService(stagesClient.Object)
            .AddService(apisClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "stage",
                "edit",
                "--api-id",
                "api-1",
                "--configuration",
                """[{"name":"dev","displayName":"Dev","conditions":[]}]""")
            .ExecuteAsync();

        // assert
        Assert.Empty(result.StdOut);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        stagesClient.VerifyAll();
        apisClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var result = await RunEditStagesWithException(
            new NitroClientGraphQLException("Some message.", "SOME_CODE"),
            InteractionMode.Interactive);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var result = await RunEditStagesWithException(
            new NitroClientGraphQLException("Some message.", "SOME_CODE"),
            InteractionMode.NonInteractive);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_JsonOutput()
    {
        // arrange
        var result = await RunEditStagesWithException(
            new NitroClientGraphQLException("Some message.", "SOME_CODE"),
            InteractionMode.JsonOutput);

        // assert
        result.AssertError(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_Interactive()
    {
        // arrange
        var result = await RunEditStagesWithException(
            new NitroClientAuthorizationException(),
            InteractionMode.Interactive);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var result = await RunEditStagesWithException(
            new NitroClientAuthorizationException(),
            InteractionMode.NonInteractive);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_JsonOutput()
    {
        // arrange
        var result = await RunEditStagesWithException(
            new NitroClientAuthorizationException(),
            InteractionMode.JsonOutput);

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);
    }

    private static async Task<CommandResult> RunEditStagesWithException(
        Exception ex,
        InteractionMode mode)
    {
        var stagesClient = new Mock<IStagesClient>(MockBehavior.Strict);
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);

        stagesClient.Setup(x => x.UpdateStagesAsync(
                "api-1",
                It.IsAny<IReadOnlyList<StageUpdateModel>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

        var result = await new CommandBuilder()
            .AddService(stagesClient.Object)
            .AddService(apisClient.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "stage",
                "edit",
                "--api-id",
                "api-1",
                "--configuration",
                """[{"name":"dev","displayName":"Dev","conditions":[]}]""")
            .ExecuteAsync();

        stagesClient.VerifyAll();
        apisClient.VerifyAll();

        return result;
    }

    [Fact]
    public async Task MutationReturnsNullApi_ReturnsSuccess_WithEmptyResult()
    {
        // arrange
        var stagesClient = new Mock<IStagesClient>(MockBehavior.Strict);
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);

        stagesClient.Setup(x => x.UpdateStagesAsync(
                "api-1",
                It.IsAny<IReadOnlyList<StageUpdateModel>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUpdateStagesPayloadWithNullApi());

        // act
        var result = await new CommandBuilder()
            .AddService(stagesClient.Object)
            .AddService(apisClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "stage",
                "edit",
                "--api-id",
                "api-1",
                "--configuration",
                """[{"name":"dev","displayName":"Dev","conditions":[]}]""")
            .ExecuteAsync();

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        stagesClient.VerifyAll();
        apisClient.VerifyAll();
    }

    public static TheoryData<IUpdateStages_UpdateStages_Errors, string> UpdateStagesMutationErrorCases =>
        new()
        {
            {
                new UpdateStages_UpdateStages_Errors_ApiNotFoundError("ApiNotFoundError", "API not found", "api-1"),
                """
                API not found
                """
            },
            {
                new UpdateStages_UpdateStages_Errors_StageNotFoundError("StageNotFoundError", "Stage not found", "production"),
                """
                Stage not found
                """
            },
            {
                new UpdateStages_UpdateStages_Errors_StagesHavePublishedDependenciesError(
                    "StagesHavePublishedDependenciesError",
                    "Stages have published dependencies",
                    Array.Empty<IUpdateStages_UpdateStages_Errors_Stages>()),
                """
                Stages have published dependencies
                """
            },
            {
                new UpdateStages_UpdateStages_Errors_StageValidationError("StageValidationError", "Stage validation failed"),
                """
                Stage validation failed
                """
            }
        };

    private static IUpdateStages_UpdateStages CreateUpdateStagesSuccessPayload()
    {
        var stage = new Mock<IUpdateStages_UpdateStages_Api_Stages>(MockBehavior.Strict);
        stage.SetupGet(x => x.Id).Returns("stage-1");
        stage.SetupGet(x => x.Name).Returns("dev");
        stage.SetupGet(x => x.DisplayName).Returns("Dev");
        stage.SetupGet(x => x.Conditions).Returns(
            Array.Empty<IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Api_Stages_Conditions>());

        var api = new Mock<IUpdateStages_UpdateStages_Api>(MockBehavior.Strict);
        api.SetupGet(x => x.Stages).Returns([stage.Object]);

        var payload = new Mock<IUpdateStages_UpdateStages>(MockBehavior.Strict);
        payload.SetupGet(x => x.Api).Returns(api.Object);
        payload.SetupGet(x => x.Errors).Returns(
            (IReadOnlyList<IUpdateStages_UpdateStages_Errors>?)null);

        return payload.Object;
    }

    private static IUpdateStages_UpdateStages CreateUpdateStagesPayloadWithNullApi()
    {
        var payload = new Mock<IUpdateStages_UpdateStages>(MockBehavior.Strict);
        payload.SetupGet(x => x.Api).Returns((IUpdateStages_UpdateStages_Api?)null);
        payload.SetupGet(x => x.Errors).Returns(
            (IReadOnlyList<IUpdateStages_UpdateStages_Errors>?)null);

        return payload.Object;
    }

    private static IUpdateStages_UpdateStages CreateUpdateStagesPayloadWithErrors(
        params IUpdateStages_UpdateStages_Errors[] errors)
    {
        var payload = new Mock<IUpdateStages_UpdateStages>(MockBehavior.Strict);
        payload.SetupGet(x => x.Api).Returns((IUpdateStages_UpdateStages_Api?)null);
        payload.SetupGet(x => x.Errors).Returns(errors);

        return payload.Object;
    }
}
