using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.PersistedOperations.InMemory;

public class InMemoryOperationDocumentStorageTests
{
    [Fact]
    public async Task Write_OperationDocument_To_Storage()
    {
        // arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddMemoryCache();
        serviceCollection.AddInMemoryOperationDocumentStorage();

        IServiceProvider services = serviceCollection.BuildServiceProvider();
        var memoryCache = services.GetRequiredService<IMemoryCache>();
        var documentStorage = services.GetRequiredService<IOperationDocumentStorage>();

        const string documentId = "abc";
        var document = Utf8GraphQLParser.Parse("{ __typename }");

        // act
        await documentStorage.SaveAsync(
            new OperationDocumentId(documentId),
            new OperationDocument(document),
            CancellationToken.None);

        // assert
        Assert.True(memoryCache.TryGetValue(documentId, out var o));
        Assert.IsType<OperationDocument>(o);
    }

    [Fact]
    public async Task Read_OperationDocument_From_Storage()
    {
        // arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddMemoryCache();
        serviceCollection.AddInMemoryOperationDocumentStorage();

        IServiceProvider services = serviceCollection.BuildServiceProvider();
        var memoryCache = services.GetRequiredService<IMemoryCache>();
        var documentStorage = services.GetRequiredService<IOperationDocumentStorage>();

        const string documentId = "abc";
        var document = Utf8GraphQLParser.Parse("{ __typename }");
        memoryCache.GetOrCreate(documentId, _ => new OperationDocument(document));

        // act
        var operationDocument = await documentStorage.TryReadAsync(
            new OperationDocumentId(documentId),
            CancellationToken.None);

        // assert
        Assert.NotNull(operationDocument);
        Assert.Same(document, Assert.IsType<OperationDocument>(operationDocument).Document);
    }
}
