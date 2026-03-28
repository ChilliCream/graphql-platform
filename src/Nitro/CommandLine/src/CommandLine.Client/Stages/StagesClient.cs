using ChilliCream.Nitro.Client.Exceptions;
using ChilliCream.Nitro.Client.Stages;

namespace ChilliCream.Nitro.Client.Stages;

internal sealed class StagesClient(IApiClient apiClient) : IStagesClient
{
    public async Task<IListStagesQuery_Node_Api> ListStagesAsync(
        string apiId,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.ListStagesQuery.ExecuteAsync(apiId, cancellationToken);
        var data = OperationResultHelper.EnsureData(result);

        return data.Node as IListStagesQuery_Node_Api
            ?? throw new NitroClientException($"Failed to list stages: API '{apiId}' was not found.");
    }

    public async Task<IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId> ForceDeleteStageAsync(
        string apiId,
        string stageName,
        CancellationToken cancellationToken)
    {
        var input = new ForceDeleteStageByApiIdInput
        {
            ApiId = apiId,
            StageName = stageName
        };

        var result = await apiClient.ForceDeleteStageByApiIdCommandMutation.ExecuteAsync(
            input,
            cancellationToken);
        return OperationResultHelper.EnsureData(result).ForceDeleteStageByApiId;
    }

    public async Task<IUpdateStages_UpdateStages> UpdateStagesAsync(
        string apiId,
        IReadOnlyList<StageUpdateModel> updatedStages,
        CancellationToken cancellationToken)
    {
        var input = new UpdateStagesInput
        {
            ApiId = apiId,
            UpdatedStages = updatedStages
                .Select(t => new StageUpdateInput
                {
                    Name = t.Name,
                    DisplayName = t.DisplayName,
                    Conditions = t.AfterStages
                        .Select(x => new StageConditionUpdateInput { AfterStage = x })
                        .ToArray()
                })
                .ToArray()
        };

        var result = await apiClient.UpdateStages.ExecuteAsync(input, cancellationToken);
        return OperationResultHelper.EnsureData(result).UpdateStages;
    }
}
