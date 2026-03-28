using ChilliCream.Nitro.Client.Exceptions;
using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.Client.Mocks;

/// <summary>
/// Provides remote mock schema operations used by mock commands.
/// </summary>
public interface IMocksClient
{
    /// <summary>
    /// Creates a mock schema.
    /// </summary>
    /// <exception cref="NitroClientException">
    /// The request failed due to transport or server-side errors.
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
    /// <exception cref="NitroClientException">
    /// The request failed due to transport or server-side errors.
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
    /// <exception cref="NitroClientException">
    /// The request failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<ConnectionPage<IListMockCommandQuery_ApiById_MockSchemas_Edges_Node>> ListMockSchemasAsync(
        string apiId,
        string? after,
        int? first,
        CancellationToken cancellationToken);
}
