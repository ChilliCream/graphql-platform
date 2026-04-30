namespace ChilliCream.Nitro.Client.Mocks;

/// <summary>
/// Provides remote mock schema operations used by mock commands.
/// </summary>
public interface IMocksClient
{
    /// <summary>
    /// Creates a mock schema.
    /// </summary>
    /// <exception cref="NitroClientGraphQLException">
    /// The server returned a GraphQL error.
    /// </exception>
    /// <exception cref="NitroClientHttpRequestException">
    /// The server returned an HTTP error without a GraphQL response body.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<ICreateMockSchema_CreateMockSchema> CreateMockSchemaAsync(
        string apiId,
        Stream baseSchemaStream,
        string downstreamUrl,
        Stream extensionStream,
        string mockSchemaName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates a mock schema.
    /// </summary>
    /// <exception cref="NitroClientGraphQLException">
    /// The server returned a GraphQL error.
    /// </exception>
    /// <exception cref="NitroClientHttpRequestException">
    /// The server returned an HTTP error without a GraphQL response body.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<IUpdateMockSchema_UpdateMockSchema> UpdateMockSchemaAsync(
        string mockSchemaId,
        Stream? baseSchemaStream,
        string? downstreamUrl,
        Stream? extensionStream,
        string? mockSchemaName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lists mock schemas for an API.
    /// </summary>
    /// <exception cref="NitroClientGraphQLException">
    /// The server returned a GraphQL error.
    /// </exception>
    /// <exception cref="NitroClientHttpRequestException">
    /// The server returned an HTTP error without a GraphQL response body.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<ConnectionPage<IListMockCommandQuery_ApiById_MockSchemas_Edges_Node>> ListMockSchemasAsync(
        string apiId,
        string? after,
        int? first,
        CancellationToken cancellationToken);
}
