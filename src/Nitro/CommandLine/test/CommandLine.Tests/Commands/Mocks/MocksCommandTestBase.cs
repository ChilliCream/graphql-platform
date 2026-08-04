using ChilliCream.Nitro.Client;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mocks;

public abstract class MocksCommandTestBase(NitroCommandFixture fixture) : CommandTestBase(fixture)
{
    protected const string MockSchemaId = "mock-1";
    protected const string MockSchemaName = "my-mock";
    protected const string UpdatedMockSchemaName = "updated-mock";
    protected const string DownstreamUrl = "https://downstream.example.com";
    protected const string ExtensionFile = "ext.graphql";
    protected const string SchemaFile = "schema.graphql";

    protected void SetupMockFiles()
    {
        SetupFile(ExtensionFile, "extension content");
        SetupFile(SchemaFile, "schema content");
    }

    #region Create

    protected void SetupCreateMockMutation(
        params ICreateMockSchema_CreateMockSchema_Errors[] errors)
    {
        SetupMockFiles();

        MocksClientMock.Setup(x => x.CreateMockSchemaAsync(
                ApiId,
                It.IsAny<Stream>(),
                DownstreamUrl,
                It.IsAny<Stream>(),
                MockSchemaName,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreateCreateMockPayload(errors));
    }

    protected void SetupCreateMockMutationException()
    {
        SetupMockFiles();

        MocksClientMock.Setup(x => x.CreateMockSchemaAsync(
                ApiId,
                It.IsAny<Stream>(),
                DownstreamUrl,
                It.IsAny<Stream>(),
                MockSchemaName,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupCreateMockMutationNullMockSchema()
    {
        SetupMockFiles();

        MocksClientMock.Setup(x => x.CreateMockSchemaAsync(
                ApiId,
                It.IsAny<Stream>(),
                DownstreamUrl,
                It.IsAny<Stream>(),
                MockSchemaName,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateCreateMockNullPayload());
    }

    #endregion

    #region Update

    protected void SetupUpdateMockMutation(
        Stream? schemaStream,
        string? downstreamUrl,
        Stream? extensionStream,
        string? name,
        params IUpdateMockSchema_UpdateMockSchema_Errors[] errors)
    {
        MocksClientMock.Setup(x => x.UpdateMockSchemaAsync(
                MockSchemaId,
                schemaStream,
                downstreamUrl,
                extensionStream,
                name,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreateUpdateMockPayload(errors));
    }

    protected void SetupUpdateMockMutationException()
    {
        MocksClientMock.Setup(x => x.UpdateMockSchemaAsync(
                MockSchemaId,
                null,
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupUpdateMockMutationNullMockSchema()
    {
        MocksClientMock.Setup(x => x.UpdateMockSchemaAsync(
                MockSchemaId,
                null,
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUpdateMockNullPayload());
    }

    protected void SetupUpdateMockMutationWithFiles(
        params IUpdateMockSchema_UpdateMockSchema_Errors[] errors)
    {
        SetupMockFiles();

        MocksClientMock.Setup(x => x.UpdateMockSchemaAsync(
                MockSchemaId,
                It.IsAny<Stream>(),
                DownstreamUrl,
                It.IsAny<Stream>(),
                UpdatedMockSchemaName,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreateUpdateMockPayload(errors));
    }

    #endregion

    #region List

    protected void SetupListMockSchemasQuery(
        string? cursor = null,
        params (string Id, string Name, string Url, Uri DownstreamUrl,
            string CreatedByUsername, DateTimeOffset CreatedAt,
            string ModifiedByUsername, DateTimeOffset ModifiedAt)[] mocks)
    {
        var items = mocks
            .Select(static m => CreateMockSchemaNode(
                m.Id, m.Name, m.Url, m.DownstreamUrl,
                m.CreatedByUsername, m.CreatedAt,
                m.ModifiedByUsername, m.ModifiedAt))
            .ToArray();

        MocksClientMock.Setup(x => x.ListMockSchemasAsync(
                ApiId,
                cursor,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionPage<IListMockCommandQuery_ApiById_MockSchemas_Edges_Node>(
                items, null, false));
    }

    protected void SetupListMockSchemasQueryWithPagination(
        string? cursor,
        string? endCursor,
        bool hasNextPage,
        params (string Id, string Name, string Url, Uri DownstreamUrl,
            string CreatedByUsername, DateTimeOffset CreatedAt,
            string ModifiedByUsername, DateTimeOffset ModifiedAt)[] mocks)
    {
        var items = mocks
            .Select(static m => CreateMockSchemaNode(
                m.Id, m.Name, m.Url, m.DownstreamUrl,
                m.CreatedByUsername, m.CreatedAt,
                m.ModifiedByUsername, m.ModifiedAt))
            .ToArray();

        MocksClientMock.Setup(x => x.ListMockSchemasAsync(
                ApiId,
                cursor,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionPage<IListMockCommandQuery_ApiById_MockSchemas_Edges_Node>(
                items, endCursor, hasNextPage));
    }

    protected void SetupListMockSchemasQueryException(string? cursor = null)
    {
        MocksClientMock.Setup(x => x.ListMockSchemasAsync(
                ApiId,
                cursor,
                10,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    #endregion

    #region Error Factories — CreateMockSchema

    protected static ICreateMockSchema_CreateMockSchema_Errors CreateCreateMockApiNotFoundError()
    {
        return new CreateMockSchema_CreateMockSchema_Errors_ApiNotFoundError(
            "ApiNotFoundError", "API not found", ApiId);
    }

    protected static ICreateMockSchema_CreateMockSchema_Errors CreateCreateMockNonUniqueNameError()
    {
        return new CreateMockSchema_CreateMockSchema_Errors_MockSchemaNonUniqueNameError(
            "MockSchemaNonUniqueNameError", "Name already in use", MockSchemaName);
    }

    protected static ICreateMockSchema_CreateMockSchema_Errors CreateCreateMockUnauthorizedError()
    {
        return new CreateMockSchema_CreateMockSchema_Errors_UnauthorizedOperation(
            "UnauthorizedOperation", "Not authorized");
    }

    protected static ICreateMockSchema_CreateMockSchema_Errors CreateCreateMockValidationError()
    {
        return new CreateMockSchema_CreateMockSchema_Errors_ValidationError(
            "ValidationError", "Validation failed", []);
    }

    #endregion

    #region Error Factories — UpdateMockSchema

    protected static IUpdateMockSchema_UpdateMockSchema_Errors CreateUpdateMockNotFoundError()
    {
        return new UpdateMockSchema_UpdateMockSchema_Errors_MockSchemaNotFoundError(
            "MockSchemaNotFoundError", "Mock schema not found");
    }

    protected static IUpdateMockSchema_UpdateMockSchema_Errors CreateUpdateMockNonUniqueNameError()
    {
        return new UpdateMockSchema_UpdateMockSchema_Errors_MockSchemaNonUniqueNameError(
            "MockSchemaNonUniqueNameError", "Name already in use", MockSchemaName);
    }

    protected static IUpdateMockSchema_UpdateMockSchema_Errors CreateUpdateMockUnauthorizedError()
    {
        return new UpdateMockSchema_UpdateMockSchema_Errors_UnauthorizedOperation(
            "UnauthorizedOperation", "Not authorized");
    }

    protected static IUpdateMockSchema_UpdateMockSchema_Errors CreateUpdateMockValidationError()
    {
        return new UpdateMockSchema_UpdateMockSchema_Errors_ValidationError(
            "ValidationError", "Validation failed", []);
    }

    #endregion

    #region Payload Factories

    private static ICreateMockSchema_CreateMockSchema CreateCreateMockPayload(
        ICreateMockSchema_CreateMockSchema_Errors[] errors)
    {
        var payload = new Mock<ICreateMockSchema_CreateMockSchema>(MockBehavior.Strict);

        if (errors.Length > 0)
        {
            payload.SetupGet(x => x.MockSchema)
                .Returns((ICreateMockSchema_CreateMockSchema_MockSchema?)null);
            payload.SetupGet(x => x.Errors).Returns(errors);
        }
        else
        {
            payload.SetupGet(x => x.MockSchema).Returns(CreateMockSchemaResult());
            payload.SetupGet(x => x.Errors)
                .Returns((IReadOnlyList<ICreateMockSchema_CreateMockSchema_Errors>?)null);
        }

        return payload.Object;
    }

    private static ICreateMockSchema_CreateMockSchema CreateCreateMockNullPayload()
    {
        var payload = new Mock<ICreateMockSchema_CreateMockSchema>(MockBehavior.Strict);
        payload.SetupGet(x => x.MockSchema)
            .Returns((ICreateMockSchema_CreateMockSchema_MockSchema?)null);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<ICreateMockSchema_CreateMockSchema_Errors>?)null);

        return payload.Object;
    }

    private static IUpdateMockSchema_UpdateMockSchema CreateUpdateMockPayload(
        IUpdateMockSchema_UpdateMockSchema_Errors[] errors)
    {
        var payload = new Mock<IUpdateMockSchema_UpdateMockSchema>(MockBehavior.Strict);

        if (errors.Length > 0)
        {
            payload.SetupGet(x => x.MockSchema)
                .Returns((IUpdateMockSchema_UpdateMockSchema_MockSchema?)null);
            payload.SetupGet(x => x.Errors).Returns(errors);
        }
        else
        {
            payload.SetupGet(x => x.MockSchema).Returns(CreateUpdateMockSchemaResult());
            payload.SetupGet(x => x.Errors)
                .Returns((IReadOnlyList<IUpdateMockSchema_UpdateMockSchema_Errors>?)null);
        }

        return payload.Object;
    }

    private static IUpdateMockSchema_UpdateMockSchema CreateUpdateMockNullPayload()
    {
        var payload = new Mock<IUpdateMockSchema_UpdateMockSchema>(MockBehavior.Strict);
        payload.SetupGet(x => x.MockSchema)
            .Returns((IUpdateMockSchema_UpdateMockSchema_MockSchema?)null);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IUpdateMockSchema_UpdateMockSchema_Errors>?)null);

        return payload.Object;
    }

    private static ICreateMockSchema_CreateMockSchema_MockSchema CreateMockSchemaResult()
    {
        var createdBy = new Mock<ICreateMockSchema_CreateMockSchema_MockSchema_CreatedBy>(MockBehavior.Strict);
        createdBy.SetupGet(x => x.Username).Returns("user1");

        var modifiedBy = new Mock<ICreateMockSchema_CreateMockSchema_MockSchema_ModifiedBy>(MockBehavior.Strict);
        modifiedBy.SetupGet(x => x.Username).Returns("user2");

        var mockSchema = new Mock<ICreateMockSchema_CreateMockSchema_MockSchema>(MockBehavior.Strict);
        mockSchema.SetupGet(x => x.Id).Returns(MockSchemaId);
        mockSchema.SetupGet(x => x.Name).Returns(MockSchemaName);
        mockSchema.SetupGet(x => x.Url).Returns("https://mock.example.com");
        mockSchema.SetupGet(x => x.DownstreamUrl).Returns(new Uri(DownstreamUrl));
        mockSchema.SetupGet(x => x.CreatedBy).Returns(createdBy.Object);
        mockSchema.SetupGet(x => x.CreatedAt).Returns(new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero));
        mockSchema.SetupGet(x => x.ModifiedBy).Returns(modifiedBy.Object);
        mockSchema.SetupGet(x => x.ModifiedAt).Returns(new DateTimeOffset(2025, 1, 16, 10, 0, 0, TimeSpan.Zero));

        return mockSchema.Object;
    }

    private static IUpdateMockSchema_UpdateMockSchema_MockSchema CreateUpdateMockSchemaResult()
    {
        var createdBy = new Mock<ICreateMockSchema_CreateMockSchema_MockSchema_CreatedBy>(MockBehavior.Strict);
        createdBy.SetupGet(x => x.Username).Returns("user1");

        var modifiedBy = new Mock<ICreateMockSchema_CreateMockSchema_MockSchema_ModifiedBy>(MockBehavior.Strict);
        modifiedBy.SetupGet(x => x.Username).Returns("user2");

        var mockSchema = new Mock<IUpdateMockSchema_UpdateMockSchema_MockSchema>(MockBehavior.Strict);
        mockSchema.SetupGet(x => x.Id).Returns(MockSchemaId);
        mockSchema.SetupGet(x => x.Name).Returns(UpdatedMockSchemaName);
        mockSchema.SetupGet(x => x.Url).Returns("https://mock.example.com");
        mockSchema.SetupGet(x => x.DownstreamUrl).Returns(new Uri(DownstreamUrl));
        mockSchema.SetupGet(x => x.CreatedBy).Returns(createdBy.Object);
        mockSchema.SetupGet(x => x.CreatedAt).Returns(new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero));
        mockSchema.SetupGet(x => x.ModifiedBy).Returns(modifiedBy.Object);
        mockSchema.SetupGet(x => x.ModifiedAt).Returns(new DateTimeOffset(2025, 1, 16, 10, 0, 0, TimeSpan.Zero));

        return mockSchema.Object;
    }

    private static IListMockCommandQuery_ApiById_MockSchemas_Edges_Node CreateMockSchemaNode(
        string id,
        string name,
        string url,
        Uri downstreamUrl,
        string createdByUsername,
        DateTimeOffset createdAt,
        string modifiedByUsername,
        DateTimeOffset modifiedAt)
    {
        var createdBy = new Mock<ICreateMockSchema_CreateMockSchema_MockSchema_CreatedBy>(MockBehavior.Strict);
        createdBy.SetupGet(x => x.Username).Returns(createdByUsername);

        var modifiedBy = new Mock<ICreateMockSchema_CreateMockSchema_MockSchema_ModifiedBy>(MockBehavior.Strict);
        modifiedBy.SetupGet(x => x.Username).Returns(modifiedByUsername);

        var node = new Mock<IListMockCommandQuery_ApiById_MockSchemas_Edges_Node>(MockBehavior.Strict);
        node.SetupGet(x => x.Id).Returns(id);
        node.SetupGet(x => x.Name).Returns(name);
        node.SetupGet(x => x.Url).Returns(url);
        node.SetupGet(x => x.DownstreamUrl).Returns(downstreamUrl);
        node.SetupGet(x => x.CreatedBy).Returns(createdBy.Object);
        node.SetupGet(x => x.CreatedAt).Returns(createdAt);
        node.SetupGet(x => x.ModifiedBy).Returns(modifiedBy.Object);
        node.SetupGet(x => x.ModifiedAt).Returns(modifiedAt);

        return node.Object;
    }

    #endregion
}
