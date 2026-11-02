namespace ChilliCream.Nitro.Client.Stages;

/// <summary>
/// Provides remote stage operations used by stage commands.
/// </summary>
public interface IStagesClient
{
    /// <summary>
    /// Loads all stages for the specified API.
    /// </summary>
    /// <returns>The stages for the API, or <c>null</c> if the API was not found.</returns>
    /// <exception cref="NitroClientGraphQLException">
    /// The server returned a GraphQL error.
    /// </exception>
    /// <exception cref="NitroClientHttpRequestException">
    /// The server returned an HTTP error without a GraphQL response body.
    /// </exception>
    /// <exception cref="NitroClientAuthorizationException">
    /// The request was rejected because the current credentials do not grant access.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<IReadOnlyList<IListStagesQuery_Node_Stages>?> ListStagesAsync(
        string apiId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Force-deletes a stage.
    /// </summary>
    /// <exception cref="NitroClientGraphQLException">
    /// The server returned a GraphQL error.
    /// </exception>
    /// <exception cref="NitroClientHttpRequestException">
    /// The server returned an HTTP error without a GraphQL response body.
    /// </exception>
    /// <exception cref="NitroClientAuthorizationException">
    /// The request was rejected because the current credentials do not grant access.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<IForceDeleteStageByApiIdCommandMutation_ForceDeleteStageByApiId> ForceDeleteStageAsync(
        string apiId,
        string stageName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates stages for an API.
    /// </summary>
    /// <exception cref="NitroClientGraphQLException">
    /// The server returned a GraphQL error.
    /// </exception>
    /// <exception cref="NitroClientHttpRequestException">
    /// The server returned an HTTP error without a GraphQL response body.
    /// </exception>
    /// <exception cref="NitroClientAuthorizationException">
    /// The request was rejected because the current credentials do not grant access.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<IUpdateStages_UpdateStages> UpdateStagesAsync(
        string apiId,
        IReadOnlyList<StageUpdateModel> updatedStages,
        CancellationToken cancellationToken);
}
