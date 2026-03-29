using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.OpenApi;

internal static class OpenApiCommandTestHelper
{
    public static ConnectionPage<IListOpenApiCollectionCommandQuery_Node_OpenApiCollections_Edges_Node> CreateListPage(
        string? endCursor = null,
        bool hasNextPage = false,
        params (string Id, string Name)[] items)
    {
        var nodes = items
            .Select(static item =>
                (IListOpenApiCollectionCommandQuery_Node_OpenApiCollections_Edges_Node)
                new ListOpenApiCollectionCommandQuery_Node_OpenApiCollections_Edges_Node_OpenApiCollection(
                    item.Id,
                    item.Name))
            .ToArray();

        return new ConnectionPage<IListOpenApiCollectionCommandQuery_Node_OpenApiCollections_Edges_Node>(
            nodes, endCursor, hasNextPage);
    }
}
