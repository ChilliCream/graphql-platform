using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Stages;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Stages;

public sealed class EditStagesCommandTests
{
    [Fact]
    public async Task Edit_MissingWorkspaceAndApi_ReturnsError()
    {
        // arrange
        var stagesClient = new Mock<IStagesClient>(MockBehavior.Strict);
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var host = CreateHost(stagesClient, apisClient, NoSession());

        // act
        var exitCode = await host.InvokeAsync(
            "stage",
            "edit",
            "--configuration",
            "[]",
            "--output",
            "json");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            You are not logged in. Run `[bold blue]nitro login[/]` to sign in or specify the workspace ID with the --workspace-id option (if available).
            """);
        stagesClient.VerifyNoOtherCalls();
        apisClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Edit_InvalidConfiguration_ReturnsError()
    {
        // arrange
        var stagesClient = new Mock<IStagesClient>(MockBehavior.Strict);
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var host = CreateHost(stagesClient, apisClient);

        // act
        var exitCode = await host.InvokeAsync(
            "stage",
            "edit",
            "--api-id",
            "api-1",
            "--configuration",
            "{invalid}");

        // assert
        Assert.NotEqual(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            Update stages

            ? For which API do you want to edit the stages?: api-1
            """);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Could not parse stage configuration
            """);
        stagesClient.VerifyNoOtherCalls();
        apisClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Edit_WithConfiguration_JsonOutput_ReturnsUpdatedStages()
    {
        // arrange
        var stagesClient = new Mock<IStagesClient>(MockBehavior.Strict);
        stagesClient.Setup(x => x.UpdateStagesAsync(
                "api-1",
                It.Is<IReadOnlyList<StageUpdateModel>>(items =>
                    items.Count == 1
                    && items[0].Name == "prod"
                    && items[0].DisplayName == "Production"
                    && items[0].AfterStages.SequenceEqual(new[] { "qa" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUpdateStagesResult(
                CreateStage("stage-1", "prod", "Production", "qa")));

        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var host = CreateHost(stagesClient, apisClient);

        const string configuration = """
            [{"name":"prod","displayName":"Production","conditions":[{"afterStage":"qa"}]}]
            """;

        // act
        var exitCode = await host.InvokeAsync(
            "stage",
            "edit",
            "--api-id",
            "api-1",
            "--configuration",
            configuration,
            "--output",
            "json");

        // assert
        Assert.Equal(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            {
              "values": [
                {
                  "id": "stage-1",
                  "name": "prod",
                  "conditions": [
                    {
                      "kind": "AfterStage",
                      "name": "qa"
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

    private static CommandBuilder CreateHost(
        Mock<IStagesClient> stagesClient,
        Mock<IApisClient> apisClient,
        TestSessionService? session = null)
    {
        var host = new CommandBuilder()
            .AddService(stagesClient.Object)
            .AddService(apisClient.Object)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

        return host;
    }

    private static TestSessionService NoSession() => new();

    private static IUpdateStages_UpdateStages CreateUpdateStagesResult(
        params IUpdateStages_UpdateStages_Api_Stages[] stages)
    {
        var api = new Mock<IUpdateStages_UpdateStages_Api_Api>();
        api.SetupGet(x => x.Stages).Returns(stages);

        var payload = new Mock<IUpdateStages_UpdateStages>();
        payload.SetupGet(x => x.Api).Returns(api.Object);
        payload.SetupGet(x => x.Errors).Returns([]);

        return payload.Object;
    }

    private static IUpdateStages_UpdateStages_Api_Stages_Stage CreateStage(
        string id,
        string name,
        string displayName,
        string? afterStage)
    {
        var stage = new Mock<IUpdateStages_UpdateStages_Api_Stages_Stage>();
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
