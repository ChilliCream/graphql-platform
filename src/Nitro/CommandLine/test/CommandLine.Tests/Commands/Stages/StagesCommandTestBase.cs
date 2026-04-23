using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Stages;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Stages;

public abstract class StagesCommandTestBase(NitroCommandFixture fixture) : CommandTestBase(fixture)
{
    protected const string StageName = "production";
    protected const string StageId = "stage-1";

    #region Edit

    protected void SetupUpdateStagesMutation(
        params IUpdateStages_UpdateStages_Errors[] errors)
    {
        StagesClientMock.Setup(x => x.UpdateStagesAsync(
                ApiId,
                It.IsAny<IReadOnlyList<StageUpdateModel>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreateUpdateStagesPayload(errors));
    }

    protected void SetupUpdateStagesMutationException()
    {
        StagesClientMock.Setup(x => x.UpdateStagesAsync(
                ApiId,
                It.IsAny<IReadOnlyList<StageUpdateModel>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupUpdateStagesMutationNullApi()
    {
        StagesClientMock.Setup(x => x.UpdateStagesAsync(
                ApiId,
                It.IsAny<IReadOnlyList<StageUpdateModel>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUpdateStagesNullApiPayload());
    }

    #endregion

    #region Delete

    protected void SetupForceDeleteStageMutation(
        params IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Errors[] errors)
    {
        StagesClientMock.Setup(x => x.ForceDeleteStageAsync(
                ApiId,
                StageName,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreateForceDeleteStagePayload(errors));
    }

    protected void SetupForceDeleteStageMutationException()
    {
        StagesClientMock.Setup(x => x.ForceDeleteStageAsync(
                ApiId,
                StageName,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupForceDeleteStageMutationNullApi()
    {
        StagesClientMock.Setup(x => x.ForceDeleteStageAsync(
                ApiId,
                StageName,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateForceDeleteStageNullApiPayload());
    }

    #endregion

    #region List

    protected void SetupListStagesQuery(
        params (string Id, string Name, string[] AfterStageNames)[] stages)
    {
        StagesClientMock.Setup(x => x.ListStagesAsync(
                ApiId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListStagesResult(stages));
    }

    protected void SetupListStagesQueryException()
    {
        StagesClientMock.Setup(x => x.ListStagesAsync(
                ApiId,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    #endregion

    #region Error Factories -- UpdateStages

    protected static IUpdateStages_UpdateStages_Errors CreateUpdateStagesApiNotFoundError()
    {
        return new UpdateStages_UpdateStages_Errors_ApiNotFoundError(
            "ApiNotFoundError", "API not found", ApiId);
    }

    protected static IUpdateStages_UpdateStages_Errors CreateUpdateStagesStageNotFoundError()
    {
        return new UpdateStages_UpdateStages_Errors_StageNotFoundError(
            "StageNotFoundError", "Stage not found", StageName);
    }

    protected static IUpdateStages_UpdateStages_Errors CreateUpdateStagesStagesHavePublishedDependenciesError()
    {
        return new UpdateStages_UpdateStages_Errors_StagesHavePublishedDependenciesError(
            "StagesHavePublishedDependenciesError",
            "Stages have published dependencies",
            Array.Empty<IUpdateStages_UpdateStages_Errors_Stages>());
    }

    protected static IUpdateStages_UpdateStages_Errors CreateUpdateStagesStageValidationError()
    {
        return new UpdateStages_UpdateStages_Errors_StageValidationError(
            "StageValidationError", "Stage validation failed");
    }

    #endregion

    #region Error Factories -- ForceDeleteStage

    protected static IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Errors CreateForceDeleteStageApiNotFoundError()
    {
        return new ForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Errors_ApiNotFoundError(
            "API not found", "ApiNotFoundError", ApiId);
    }

    protected static IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Errors CreateForceDeleteStageStageNotFoundError()
    {
        return new ForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Errors_StageNotFoundError(
            "Stage not found", "StageNotFoundError", StageName);
    }

    protected static IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Errors CreateForceDeleteStageUnauthorizedError()
    {
        return new ForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Errors_UnauthorizedOperation(
            "Not authorized", "UnauthorizedOperation");
    }

    #endregion

    #region Payload Factories

    private static IUpdateStages_UpdateStages CreateUpdateStagesPayload(
        IUpdateStages_UpdateStages_Errors[] errors)
    {
        var payload = new Mock<IUpdateStages_UpdateStages>(MockBehavior.Strict);

        if (errors.Length > 0)
        {
            payload.SetupGet(x => x.Api).Returns((IUpdateStages_UpdateStages_Api?)null);
            payload.SetupGet(x => x.Errors).Returns(errors);
        }
        else
        {
            var stage = new Mock<IUpdateStages_UpdateStages_Api_Stages>(MockBehavior.Strict);
            stage.SetupGet(x => x.Id).Returns(StageId);
            stage.SetupGet(x => x.Name).Returns("dev");
            stage.SetupGet(x => x.DisplayName).Returns("Dev");
            stage.SetupGet(x => x.Conditions).Returns(
                Array.Empty<IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Api_Stages_Conditions>());

            var api = new Mock<IUpdateStages_UpdateStages_Api>(MockBehavior.Strict);
            api.SetupGet(x => x.Stages).Returns([stage.Object]);

            payload.SetupGet(x => x.Api).Returns(api.Object);
            payload.SetupGet(x => x.Errors).Returns(
                (IReadOnlyList<IUpdateStages_UpdateStages_Errors>?)null);
        }

        return payload.Object;
    }

    private static IUpdateStages_UpdateStages CreateUpdateStagesNullApiPayload()
    {
        var payload = new Mock<IUpdateStages_UpdateStages>(MockBehavior.Strict);
        payload.SetupGet(x => x.Api).Returns((IUpdateStages_UpdateStages_Api?)null);
        payload.SetupGet(x => x.Errors).Returns(
            (IReadOnlyList<IUpdateStages_UpdateStages_Errors>?)null);

        return payload.Object;
    }

    private static IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId CreateForceDeleteStagePayload(
        IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Errors[] errors)
    {
        var payload = new Mock<IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId>(MockBehavior.Strict);

        if (errors.Length > 0)
        {
            payload.SetupGet(x => x.Api).Returns(
                (IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Api?)null);
            payload.SetupGet(x => x.Errors).Returns(errors);
        }
        else
        {
            var stage = new Mock<IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Api_Stages>(MockBehavior.Strict);
            stage.SetupGet(x => x.Id).Returns(StageId);
            stage.SetupGet(x => x.Name).Returns("dev");
            stage.SetupGet(x => x.DisplayName).Returns("Development");
            stage.SetupGet(x => x.Conditions).Returns(
                Array.Empty<IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Api_Stages_Conditions>());

            var api = new Mock<IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Api>(MockBehavior.Strict);
            api.SetupGet(x => x.Stages).Returns([stage.Object]);

            payload.SetupGet(x => x.Api).Returns(api.Object);
            payload.SetupGet(x => x.Errors).Returns(
                (IReadOnlyList<IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Errors>?)null);
        }

        return payload.Object;
    }

    private static IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId CreateForceDeleteStageNullApiPayload()
    {
        var payload = new Mock<IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId>(MockBehavior.Strict);
        payload.SetupGet(x => x.Api).Returns(
            (IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Api?)null);
        payload.SetupGet(x => x.Errors).Returns(
            (IReadOnlyList<IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId_Errors>?)null);

        return payload.Object;
    }

    private static IReadOnlyList<IListStagesQuery_Node_Stages> CreateListStagesResult(
        (string Id, string Name, string[] AfterStageNames)[] stages)
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

    #endregion
}
