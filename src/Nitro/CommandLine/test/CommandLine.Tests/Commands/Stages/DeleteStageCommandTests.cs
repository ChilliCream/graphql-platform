using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Stages;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Stages;

public sealed class DeleteStageCommandTests
{
    [Fact]
    public async Task Delete_MissingStageOption_ReturnsParseError()
    {
        // arrange
        var stagesClient = new Mock<IStagesClient>(MockBehavior.Strict);
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var host = CreateHost(stagesClient, apisClient);

        // act
        var exitCode = await host.InvokeAsync(
            "stage",
            "delete",
            "--api-id",
            "api-1",
            "--force");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Required option missing for command: '--stage'.
            """);
        stagesClient.VerifyNoOtherCalls();
        apisClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_WithForce_JsonOutput_ReturnsRemainingStages()
    {
        // arrange
        var stagesClient = new Mock<IStagesClient>(MockBehavior.Strict);
        stagesClient.Setup(x => x.ForceDeleteStageAsync("api-1", "prod", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateForceDeleteResult(
                CreateStage("stage-2", "qa", "QA", "default")));

        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var host = CreateHost(stagesClient, apisClient);

        // act
        var exitCode = await host.InvokeAsync(
            "stage",
            "delete",
            "--api-id",
            "api-1",
            "--stage",
            "prod",
            "--force",
            "--output",
            "json");

        // assert
        Assert.Equal(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            {
              "values": [
                {
                  "id": "stage-2",
                  "name": "qa",
                  "conditions": [
                    {
                      "kind": "AfterStage",
                      "name": "default"
                    }
                  ]
                }
              ],
              "cursor": null
            }
            """);
        Assert.Empty(host.StdErr);
        stagesClient.VerifyAll();
        apisClient.VerifyNoOtherCalls();
    }

    private static CommandTestHost CreateHost(
        Mock<IStagesClient> stagesClient,
        Mock<IApisClient> apisClient,
        TestSessionService? session = null)
    {
        var host = new CommandTestHost()
            .AddService(stagesClient.Object)
            .AddService(apisClient.Object)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

        return host;
    }

    private static IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId CreateForceDeleteResult(
        params IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Api_Stages[] stages)
    {
        var api = new Mock<IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Api_Api>();
        api.SetupGet(x => x.Stages).Returns(stages);

        var payload = new Mock<IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId>();
        payload.SetupGet(x => x.Api).Returns(api.Object);
        payload.SetupGet(x => x.Errors).Returns([]);

        return payload.Object;
    }

    private static IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Api_Stages_Stage CreateStage(
        string id,
        string name,
        string displayName,
        string? afterStage)
    {
        var stage = new Mock<IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Api_Stages_Stage>();
        stage.SetupGet(x => x.Id).Returns(id);
        stage.SetupGet(x => x.Name).Returns(name);
        stage.SetupGet(x => x.DisplayName).Returns(displayName);
        stage.SetupGet(x => x.Conditions).Returns(CreateConditions(afterStage));

        return stage.Object;
    }

    private static IReadOnlyList<IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Api_Stages_Conditions>
        CreateConditions(string? afterStage)
    {
        if (afterStage is null)
        {
            return [];
        }

        var stage = new Mock<IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Api_Stages_Conditions_AfterStage_Stage>();
        stage.SetupGet(x => x.Name).Returns(afterStage);

        var condition = new Mock<IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Api_Stages_Conditions_AfterStageCondition>();
        condition.SetupGet(x => x.AfterStage).Returns(stage.Object);

        return [condition.Object];
    }
}
