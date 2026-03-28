namespace ChilliCream.Nitro.Client.Stages;

/// <summary>
/// Provides remote stage operations used by stage commands.
/// </summary>
public interface IStagesClient
{
    /// <summary>
    /// Loads all stages for the specified API.
    /// </summary>
    /// <exception cref="ChilliCream.Nitro.Client.Exceptions.NitroClientException">
    /// The request failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<IListStagesQuery_Node_Api> ListStagesAsync(
        string apiId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Force-deletes a stage.
    /// </summary>
    /// <exception cref="ChilliCream.Nitro.Client.Exceptions.NitroClientException">
    /// The request failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId> ForceDeleteStageAsync(
        string apiId,
        string stageName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates stages for an API.
    /// </summary>
    /// <exception cref="ChilliCream.Nitro.Client.Exceptions.NitroClientException">
    /// The request failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<IUpdateStages_UpdateStages> UpdateStagesAsync(
        string apiId,
        IReadOnlyList<StageUpdateModel> updatedStages,
        CancellationToken cancellationToken);
}
