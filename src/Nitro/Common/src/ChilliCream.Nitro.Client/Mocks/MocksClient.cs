using StrawberryShake;

namespace ChilliCream.Nitro.Client.Mocks;

internal sealed class MocksClient(IApiClient apiClient) : IMocksClient
{
    public async Task<ICreateMockSchema_CreateMockSchema> CreateMockSchemaAsync(
        string apiId,
        Stream baseSchemaStream,
        string downstreamUrl,
        Stream extensionStream,
        string mockSchemaName,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.CreateMockSchema.ExecuteAsync(
            apiId,
            new Upload(baseSchemaStream, "schema.graphql"),
            downstreamUrl,
            new Upload(extensionStream, "extension.graphql"),
            mockSchemaName,
            cancellationToken);

        return OperationResultHelper.EnsureData(result).CreateMockSchema;
    }

    public async Task<IUpdateMockSchema_UpdateMockSchema> UpdateMockSchemaAsync(
        string mockSchemaId,
        Stream? baseSchemaStream,
        string? downstreamUrl,
        Stream? extensionStream,
        string? mockSchemaName,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.UpdateMockSchema.ExecuteAsync(
            mockSchemaId,
            baseSchemaStream is null ? null : new Upload(baseSchemaStream, "schema.graphql"),
            downstreamUrl,
            extensionStream is null ? null : new Upload(extensionStream, "extension.graphql"),
            mockSchemaName,
            cancellationToken);

        return OperationResultHelper.EnsureData(result).UpdateMockSchema;
    }

    public async Task<ConnectionPage<IListMockCommandQuery_ApiById_MockSchemas_Edges_Node>> ListMockSchemasAsync(
        string apiId,
        string? after,
        int? first,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.ListMockCommandQuery.ExecuteAsync(apiId, after, first, cancellationToken);

        var data = OperationResultHelper.EnsureData(result);
        var connection = data.ApiById?.MockSchemas;
        var items = connection?.Edges?.Select(static edge => edge.Node).ToArray() ?? [];

        return new ConnectionPage<IListMockCommandQuery_ApiById_MockSchemas_Edges_Node>(
            items,
            connection?.PageInfo.EndCursor,
            connection?.PageInfo.HasNextPage ?? false);
    }
}
