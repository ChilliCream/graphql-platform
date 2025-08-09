using HotChocolate.Language;

namespace HotChocolate.ModelContextProtocol.Storage;

public sealed class InMemoryMcpOperationDocumentStorageTests
{
    [Fact]
    public async Task SaveToolDocumentAsync_DocumentWithNoOperations_ThrowsException()
    {
        // arrange & act
        static async Task Action()
        {
            var storage = new InMemoryMcpOperationDocumentStorage();
            var document = Utf8GraphQLParser.Parse("fragment Fragment on Type { field }");

            await storage.SaveToolDocumentAsync(document);
        }

        // assert
        Assert.Equal(
            "A tool document must contain a single operation definition.",
            (await Assert.ThrowsAsync<Exception>(Action)).Message);
    }

    [Fact]
    public async Task SaveToolDocumentAsync_DocumentWithMultipleOperations_ThrowsException()
    {
        // arrange & act
        static async Task Action()
        {
            var storage = new InMemoryMcpOperationDocumentStorage();
            var document = Utf8GraphQLParser.Parse(
                """
                query Operation1 { query { field } }
                query Operation2 { query { field } }
                """);

            await storage.SaveToolDocumentAsync(document);
        }

        // assert
        Assert.Equal(
            "A tool document must contain a single operation definition.",
            (await Assert.ThrowsAsync<Exception>(Action)).Message);
    }

    [Fact]
    public async Task SaveToolDocumentAsync_DocumentWithUnnamedOperation_ThrowsException()
    {
        // arrange & act
        static async Task Action()
        {
            var storage = new InMemoryMcpOperationDocumentStorage();
            var document = Utf8GraphQLParser.Parse("query { query { field } }");

            await storage.SaveToolDocumentAsync(document);
        }

        // assert
        Assert.Equal(
            "A tool document operation must be named.",
            (await Assert.ThrowsAsync<Exception>(Action)).Message);
    }

    [Fact]
    public async Task SaveToolDocumentAsync_DocumentWithNonUniqueOperationName_ThrowsException()
    {
        // arrange & act
        static async Task Action()
        {
            var storage = new InMemoryMcpOperationDocumentStorage();
            var document1 = Utf8GraphQLParser.Parse("query Operation1 { query1 { field } }");
            var document2 = Utf8GraphQLParser.Parse("query Operation1 { query2 { field } }");

            await storage.SaveToolDocumentAsync(document1);
            await storage.SaveToolDocumentAsync(document2);
        }

        // assert
        Assert.Equal(
            "A tool document operation with the same name already exists.",
            (await Assert.ThrowsAsync<Exception>(Action)).Message);
    }
}
