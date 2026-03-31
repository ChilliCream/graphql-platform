using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Stages;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Stages;

public sealed class DeleteStageCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "stage",
                "delete",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Delete a stage by name.

            Usage:
              nitro stage delete [options]

            Options:
              --api-id <api-id>           The ID of the API [env: NITRO_API_ID]
              --stage <stage> (REQUIRED)  The name of the stage [env: NITRO_STAGE]
              --force                     Skip confirmation prompts for deletes and overwrites
              --cloud-url <cloud-url>     The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>         The API key used for authentication [env: NITRO_API_KEY]
              --output <json>             The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help              Show help and usage information
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
                "delete",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--force")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run
            'nitro login'.
            """);
    }

    [Fact]
    public async Task WithoutForce_And_ConfirmationRejected_ReturnsError()
    {
        // arrange
        var stagesClient = new Mock<IStagesClient>(MockBehavior.Strict);
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);

        var command = new CommandBuilder(fixture)
            .AddService(stagesClient.Object)
            .AddService(apisClient.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "stage",
                "delete",
                "--api-id",
                "api-1",
                "--stage",
                "production")
            .Start();

        // act
        command.Confirm(false);
        var result = await command.RunToCompletionAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            ? For which API do you want to force delete a stage?: api-1
            ? Do you really want to force delete stage production [y/n] (y): n
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Stage was not deleted.
            """);
        Assert.Equal(1, result.ExitCode);

        stagesClient.VerifyAll();
        apisClient.VerifyAll();
    }

    [Fact]
    public async Task WithConfirmation_ReturnsSuccess()
    {
        // arrange
        var stagesClient = new Mock<IStagesClient>(MockBehavior.Strict);
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);

        stagesClient.Setup(x => x.ForceDeleteStageAsync(
                "api-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDeleteStageSuccessPayload());

        var command = new CommandBuilder(fixture)
            .AddService(stagesClient.Object)
            .AddService(apisClient.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "stage",
                "delete",
                "--api-id",
                "api-1",
                "--stage",
                "production")
            .Start();

        // act
        command.Confirm(true);
        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        stagesClient.VerifyAll();
        apisClient.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullApi_ReturnsError()
    {
        // arrange
        var stagesClient = new Mock<IStagesClient>(MockBehavior.Strict);
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);

        stagesClient.Setup(x => x.ForceDeleteStageAsync(
                "api-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDeleteStagePayloadWithNullApi());

        var command = new CommandBuilder(fixture)
            .AddService(stagesClient.Object)
            .AddService(apisClient.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "stage",
                "delete",
                "--api-id",
                "api-1",
                "--stage",
                "production")
            .Start();

        // act
        command.Confirm(true);
        var result = await command.RunToCompletionAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the
            expected data.
            """);
        Assert.Equal(1, result.ExitCode);

        stagesClient.VerifyAll();
        apisClient.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(DeleteStageMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError(
        IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var stagesClient = new Mock<IStagesClient>(MockBehavior.Strict);
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);

        stagesClient.Setup(x => x.ForceDeleteStageAsync(
                "api-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDeleteStagePayloadWithErrors(mutationError));

        var command = new CommandBuilder(fixture)
            .AddService(stagesClient.Object)
            .AddService(apisClient.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "stage",
                "delete",
                "--api-id",
                "api-1",
                "--stage",
                "production")
            .Start();

        // act
        command.Confirm(true);
        var result = await command.RunToCompletionAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        stagesClient.VerifyAll();
        apisClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError()
    {
        // arrange
        var stagesClient = new Mock<IStagesClient>(MockBehavior.Strict);
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);

        stagesClient.Setup(x => x.ForceDeleteStageAsync(
                "api-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        var command = new CommandBuilder(fixture)
            .AddService(stagesClient.Object)
            .AddService(apisClient.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "stage",
                "delete",
                "--api-id",
                "api-1",
                "--stage",
                "production")
            .Start();

        // act
        command.Confirm(true);
        var result = await command.RunToCompletionAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        stagesClient.VerifyAll();
        apisClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError()
    {
        // arrange
        var stagesClient = new Mock<IStagesClient>(MockBehavior.Strict);
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);

        stagesClient.Setup(x => x.ForceDeleteStageAsync(
                "api-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException());

        var command = new CommandBuilder(fixture)
            .AddService(stagesClient.Object)
            .AddService(apisClient.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "stage",
                "delete",
                "--api-id",
                "api-1",
                "--stage",
                "production")
            .Start();

        // act
        command.Confirm(true);
        var result = await command.RunToCompletionAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        stagesClient.VerifyAll();
        apisClient.VerifyAll();
    }

    public static TheoryData<IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Errors, string> DeleteStageMutationErrorCases =>
        new()
        {
            {
                new ForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Errors_ApiNotFoundError("API not found", "ApiNotFoundError", "api-1"),
                """
                API not found
                """
            },
            {
                new ForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Errors_StageNotFoundError("Stage not found", "StageNotFoundError", "production"),
                """
                Stage not found
                """
            },
            {
                new ForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Errors_UnauthorizedOperation("Not authorized", "UnauthorizedOperation"),
                """
                Not authorized
                """
            }
        };

    private static IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId CreateDeleteStageSuccessPayload()
    {
        var stage = new Mock<IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Api_Stages>(MockBehavior.Strict);
        stage.SetupGet(x => x.Id).Returns("stage-1");
        stage.SetupGet(x => x.Name).Returns("dev");
        stage.SetupGet(x => x.DisplayName).Returns("Development");
        stage.SetupGet(x => x.Conditions).Returns(
            Array.Empty<IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Api_Stages_Conditions>());

        var api = new Mock<IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Api>(MockBehavior.Strict);
        api.SetupGet(x => x.Stages).Returns([stage.Object]);

        var payload = new Mock<IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId>(MockBehavior.Strict);
        payload.SetupGet(x => x.Api).Returns(api.Object);
        payload.SetupGet(x => x.Errors).Returns(
            (IReadOnlyList<IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Errors>?)null);

        return payload.Object;
    }

    private static IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId CreateDeleteStagePayloadWithNullApi()
    {
        var payload = new Mock<IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId>(MockBehavior.Strict);
        payload.SetupGet(x => x.Api).Returns((IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Api?)null);
        payload.SetupGet(x => x.Errors).Returns(
            (IReadOnlyList<IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Errors>?)null);

        return payload.Object;
    }

    private static IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId CreateDeleteStagePayloadWithErrors(
        params IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Errors[] errors)
    {
        var payload = new Mock<IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId>(MockBehavior.Strict);
        payload.SetupGet(x => x.Api).Returns((IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Api?)null);
        payload.SetupGet(x => x.Errors).Returns(errors);

        return payload.Object;
    }
}
