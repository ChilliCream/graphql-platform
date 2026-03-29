using ChilliCream.Nitro.Client;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp;

internal static class McpCommandTestHelper
{
    public static ConnectionPage<IListMcpFeatureCollectionCommandQuery_Node_McpFeatureCollections_Edges_Node> CreateListPage(
        string? endCursor = null,
        bool hasNextPage = false,
        params (string Id, string Name)[] items)
    {
        var nodes = items
            .Select(static item =>
                (IListMcpFeatureCollectionCommandQuery_Node_McpFeatureCollections_Edges_Node)
                new ListMcpFeatureCollectionCommandQuery_Node_McpFeatureCollections_Edges_Node_McpFeatureCollection(
                    item.Id,
                    item.Name))
            .ToArray();

        return new ConnectionPage<IListMcpFeatureCollectionCommandQuery_Node_McpFeatureCollections_Edges_Node>(
            nodes, endCursor, hasNextPage);
    }

    // Create mutation helpers

    public static ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection CreateMcpFeatureCollectionPayload(
        string id,
        string name)
    {
        var payload = new Mock<ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.McpFeatureCollection)
            .Returns(new CreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection_McpFeatureCollection_McpFeatureCollection(name, id));
        payload.SetupGet(x => x.Errors)
            .Returns(Array.Empty<ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection_Errors>());
        return payload.Object;
    }

    public static ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection CreateMcpFeatureCollectionPayloadWithNullResult()
    {
        var payload = new Mock<ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.McpFeatureCollection)
            .Returns((ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection_McpFeatureCollection?)null);
        payload.SetupGet(x => x.Errors)
            .Returns(Array.Empty<ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection_Errors>());
        return payload.Object;
    }

    public static ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection CreateMcpFeatureCollectionPayloadWithErrors(
        params ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection_Errors[] errors)
    {
        var payload = new Mock<ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.McpFeatureCollection)
            .Returns((ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection_McpFeatureCollection?)null);
        payload.SetupGet(x => x.Errors).Returns(errors);
        return payload.Object;
    }

    // Delete mutation helpers

    public static IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById CreateDeleteMcpFeatureCollectionPayload(
        string id,
        string name)
    {
        var payload = new Mock<IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById>(MockBehavior.Strict);
        payload.SetupGet(x => x.McpFeatureCollection)
            .Returns(new DeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById_McpFeatureCollection_McpFeatureCollection(name, id));
        payload.SetupGet(x => x.Errors)
            .Returns(Array.Empty<IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById_Errors>());
        return payload.Object;
    }

    public static IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById CreateDeleteMcpFeatureCollectionPayloadWithNullResult()
    {
        var payload = new Mock<IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById>(MockBehavior.Strict);
        payload.SetupGet(x => x.McpFeatureCollection)
            .Returns((IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById_McpFeatureCollection?)null);
        payload.SetupGet(x => x.Errors)
            .Returns(Array.Empty<IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById_Errors>());
        return payload.Object;
    }

    public static IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById CreateDeleteMcpFeatureCollectionPayloadWithErrors(
        params IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById_Errors[] errors)
    {
        var payload = new Mock<IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById>(MockBehavior.Strict);
        payload.SetupGet(x => x.McpFeatureCollection)
            .Returns((IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById_McpFeatureCollection?)null);
        payload.SetupGet(x => x.Errors).Returns(errors);
        return payload.Object;
    }
}
