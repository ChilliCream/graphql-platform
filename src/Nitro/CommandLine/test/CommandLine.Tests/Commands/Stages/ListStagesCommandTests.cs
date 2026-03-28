using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Stages;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Stages;

public sealed class ListStagesCommandTests
{
    [Fact]
    public async Task List_MissingApiId_InNonInteractiveMode_ReturnsError()
    {
        // arrange
        var stagesClient = new Mock<IStagesClient>(MockBehavior.Strict);
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var host = CreateHost(stagesClient, apisClient);

        // act
        var exitCode = await host.InvokeAsync(
            "stage",
            "list",
            "--output",
            "json");

        // assert
        Assert.Equal(1, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            The API ID is required in non-interactive mode.
            """);
        stagesClient.VerifyNoOtherCalls();
        apisClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task List_NonInteractive_JsonOutput_ReturnsPaginatedResult()
    {
        // arrange
        var stagesClient = new Mock<IStagesClient>(MockBehavior.Strict);
        stagesClient.Setup(x => x.ListStagesAsync("api-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListStagesResult(
                CreateListStage("stage-1", "prod", "Production", "qa")));

        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var host = CreateHost(stagesClient, apisClient);

        // act
        var exitCode = await host.InvokeAsync(
            "stage",
            "list",
            "--api-id",
            "api-1",
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

    [Fact]
    public async Task List_InteractivePath_UsesSelectableTableBranch()
    {
        // arrange
        var stagesClient = new Mock<IStagesClient>(MockBehavior.Strict);
        stagesClient.Setup(x => x.ListStagesAsync("api-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListStagesResult());

        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var host = CreateHost(stagesClient, apisClient);
        host.Console.Input.PushKey(ConsoleKey.Escape);

        // act
        var exitCode = await host.InvokeAsync(
            "stage",
            "list",
            "--api-id",
            "api-1");

        // assert
        Assert.Equal(0, exitCode);
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

    private static IListStagesQuery_Node_Api CreateListStagesResult(
        params IListStagesQuery_Node_Stages[] stages)
    {
        var api = new Mock<IListStagesQuery_Node_Api>();
        api.SetupGet(x => x.Stages).Returns(stages);
        return api.Object;
    }

    private static IListStagesQuery_Node_Stages_Stage CreateListStage(
        string id,
        string name,
        string displayName,
        string? afterStage)
    {
        var stage = new Mock<IListStagesQuery_Node_Stages_Stage>();
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
