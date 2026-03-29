using ChilliCream.Nitro.Client;
using Moq;

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

    // Create mutation helpers

    public static ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection CreateOpenApiCollectionPayload(
        string id,
        string name)
    {
        var payload = new Mock<ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.OpenApiCollection)
            .Returns(new CreateOpenApiCollectionCommandMutation_CreateOpenApiCollection_OpenApiCollection_OpenApiCollection(name, id));
        payload.SetupGet(x => x.Errors)
            .Returns(Array.Empty<ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection_Errors>());
        return payload.Object;
    }

    public static ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection CreateOpenApiCollectionPayloadWithNullResult()
    {
        var payload = new Mock<ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.OpenApiCollection)
            .Returns((ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection_OpenApiCollection?)null);
        payload.SetupGet(x => x.Errors)
            .Returns(Array.Empty<ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection_Errors>());
        return payload.Object;
    }

    public static ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection CreateOpenApiCollectionPayloadWithErrors(
        params ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection_Errors[] errors)
    {
        var payload = new Mock<ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.OpenApiCollection)
            .Returns((ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection_OpenApiCollection?)null);
        payload.SetupGet(x => x.Errors).Returns(errors);
        return payload.Object;
    }

    // Delete mutation helpers

    public static IDeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById CreateDeleteOpenApiCollectionPayload(
        string id,
        string name)
    {
        var payload = new Mock<IDeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById>(MockBehavior.Strict);
        payload.SetupGet(x => x.OpenApiCollection)
            .Returns(new DeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById_OpenApiCollection_OpenApiCollection(name, id));
        payload.SetupGet(x => x.Errors)
            .Returns(Array.Empty<IDeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById_Errors>());
        return payload.Object;
    }

    public static IDeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById CreateDeleteOpenApiCollectionPayloadWithNullResult()
    {
        var payload = new Mock<IDeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById>(MockBehavior.Strict);
        payload.SetupGet(x => x.OpenApiCollection)
            .Returns((IDeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById_OpenApiCollection?)null);
        payload.SetupGet(x => x.Errors)
            .Returns(Array.Empty<IDeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById_Errors>());
        return payload.Object;
    }

    public static IDeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById CreateDeleteOpenApiCollectionPayloadWithErrors(
        params IDeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById_Errors[] errors)
    {
        var payload = new Mock<IDeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById>(MockBehavior.Strict);
        payload.SetupGet(x => x.OpenApiCollection)
            .Returns((IDeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById_OpenApiCollection?)null);
        payload.SetupGet(x => x.Errors).Returns(errors);
        return payload.Object;
    }
}
