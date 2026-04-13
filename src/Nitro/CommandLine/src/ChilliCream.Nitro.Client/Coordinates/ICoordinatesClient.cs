namespace ChilliCream.Nitro.Client.Coordinates;

/// <summary>
/// Provides the remote coordinate analytics queries used by the
/// <c>nitro schema usage</c>, <c>clients</c>, <c>operations</c>, <c>unused</c>, and
/// <c>impact</c> commands.
/// </summary>
public interface ICoordinatesClient
{
    /// <summary>
    /// Loads the aggregate usage metrics for a single coordinate on the given stage.
    /// </summary>
    /// <returns>
    /// The matching stage node, or <see langword="null"/> if the API or stage could not
    /// be resolved.
    /// </returns>
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
    Task<ICoordinateUsageQuery_ApiById_Stages?> GetCoordinateUsageAsync(
        string apiId,
        string stageName,
        string coordinate,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads the per-client usage breakdown for a single coordinate on the given stage.
    /// </summary>
    /// <returns>
    /// The matching stage node, or <see langword="null"/> if the API or stage could not
    /// be resolved.
    /// </returns>
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
    Task<ICoordinateClientsQuery_ApiById_Stages?> GetCoordinateClientsAsync(
        string apiId,
        string stageName,
        string coordinate,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads the per-operation usage breakdown for a single coordinate on the given stage,
    /// flattened across clients.
    /// </summary>
    /// <returns>
    /// The matching stage node, or <see langword="null"/> if the API or stage could not
    /// be resolved.
    /// </returns>
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
    Task<ICoordinateOperationsQuery_ApiById_Stages?> GetCoordinateOperationsAsync(
        string apiId,
        string stageName,
        string coordinate,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads the combined impact payload (clients + operations + usage + deprecation) for a
    /// single coordinate on the given stage.
    /// </summary>
    /// <returns>
    /// The matching stage node, or <see langword="null"/> if the API or stage could not
    /// be resolved.
    /// </returns>
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
    Task<ICoordinateImpactQuery_ApiById_Stages?> GetCoordinateImpactAsync(
        string apiId,
        string stageName,
        string coordinate,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads the sorted unused coordinates for the given stage and kind filter.
    /// </summary>
    /// <returns>
    /// The matching stage node, or <see langword="null"/> if the API or stage could not
    /// be resolved.
    /// </returns>
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
    Task<IUnusedCoordinatesQuery_ApiById_Stages?> GetUnusedCoordinatesAsync(
        string apiId,
        string stageName,
        DateTimeOffset from,
        DateTimeOffset to,
        IReadOnlyList<CoordinateKind>? kinds,
        bool? isDeprecated,
        int first,
        CancellationToken cancellationToken);
}
