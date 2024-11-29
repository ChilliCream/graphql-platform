using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using Squadron;
using StackExchange.Redis;

namespace HotChocolate.PersistedOperations.Redis;

public class RedisOperationDocumentStorageTests(RedisResource redisResource)
    : IClassFixture<RedisResource>
{
    private readonly IDatabase _database = redisResource.GetConnection().GetDatabase();

    [Fact]
    public async Task Write_OperationDocument_To_Storage()
    {
        // arrange
        var documentId = new OperationDocumentId(Guid.NewGuid().ToString("N"));
        var storage = new RedisOperationDocumentStorage(_database);
        var document = new OperationDocumentSourceText("{ foo }");

        // act
        await storage.SaveAsync(documentId, document);

        // assert
        var buffer = ((byte[])await _database.StringGetAsync(documentId.Value))!;
        Utf8GraphQLParser.Parse(buffer).Print().MatchSnapshot();

        await _database.KeyDeleteAsync(documentId.Value);
    }

    [Fact]
    public async Task Write_OperationDocument_documentId_Invalid()
    {
        // arrange
        var documentId = new OperationDocumentId();
        var storage = new RedisOperationDocumentStorage(_database);
        var document = new OperationDocumentSourceText("{ foo }");

        // act
        async Task Action() => await storage.SaveAsync(documentId, document);

        // assert
        await Assert.ThrowsAsync<ArgumentNullException>(Action);
    }

    [Fact]
    public async Task Write_OperationDocument_OperationDocument_Is_Null()
    {
        // arrange
        var storage = new RedisOperationDocumentStorage(_database);
        var documentId = new OperationDocumentId(Guid.NewGuid().ToString("N"));

        // act
        async Task Action() => await storage.SaveAsync(documentId, null!);

        // assert
        await Assert.ThrowsAsync<ArgumentNullException>(Action);
    }

    [Fact]
    public async Task Read_OperationDocument_From_Storage()
    {
        // arrange
        var documentId = new OperationDocumentId(Guid.NewGuid().ToString("N"));
        var storage = new RedisOperationDocumentStorage(_database);
        var buffer = "{ foo }"u8.ToArray();
        await _database.StringSetAsync(documentId.Value, buffer);

        // act
        var document = await storage.TryReadAsync(documentId);

        // assert
        Assert.NotNull(document);
        Assert.IsType<OperationDocument>(document).Document.Print().MatchSnapshot();

        await _database.KeyDeleteAsync(documentId.Value);
    }

    [Fact]
    public async Task Read_OperationDocument_documentId_Invalid()
    {
        // arrange
        var documentId = new OperationDocumentId();
        var storage = new RedisOperationDocumentStorage(_database);

        // act
        async Task Action() => await storage.TryReadAsync(documentId);

        // assert
        await Assert.ThrowsAsync<ArgumentNullException>(Action);
    }
}
