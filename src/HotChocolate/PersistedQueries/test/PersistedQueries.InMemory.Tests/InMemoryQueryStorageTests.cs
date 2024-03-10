using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.PersistedQueries.FileSystem;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.PersistedQueries.InMemory;

public class InMemoryQueryStorageTests
{
    [Fact]
    public async Task Write_Query_To_Storage()
    {
        // arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddMemoryCache();
        serviceCollection.AddInMemoryOperationDocumentStorage();

        IServiceProvider services = serviceCollection.BuildServiceProvider();
        var memoryCache = services.GetRequiredService<IMemoryCache>();
        var queryStorage = services.GetRequiredService<IOperationDocumentStorage>();

        const string queryId = "abc";
        var query = Utf8GraphQLParser.Parse("{ __typename }");

        // act
        await queryStorage.SaveAsync(
            new OperationDocumentId(queryId),
            new OperationDocument(query),
            CancellationToken.None);

        // assert
        Assert.True(memoryCache.TryGetValue(queryId, out var o));
        Assert.IsType<OperationDocument>(o);
    }

    [Fact]
    public async Task Read_Query_From_Storage()
    {
        // arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddMemoryCache();
        serviceCollection.AddInMemoryOperationDocumentStorage();

        IServiceProvider services = serviceCollection.BuildServiceProvider();
        var memoryCache = services.GetRequiredService<IMemoryCache>();
        var queryStorage = services.GetRequiredService<IOperationDocumentStorage>();

        const string queryId = "abc";
        var query = Utf8GraphQLParser.Parse("{ __typename }");
        memoryCache.GetOrCreate(queryId, _ => new OperationDocument(query));

        // act
        var document = await queryStorage.TryReadAsync(
            new OperationDocumentId(queryId),
            CancellationToken.None);

        // assert
        Assert.NotNull(document);
        Assert.Same(query, Assert.IsType<OperationDocument>(document).Document);
    }
}
