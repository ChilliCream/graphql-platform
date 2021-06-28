using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.PersistedQueries.FileSystem
{
    public class InMemoryQueryStorageTests
    {
        [Fact]
        public async Task Write_Query_To_Storage()
        {
            // arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddMemoryCache();
            serviceCollection.AddInMemoryQueryStorage();

            IServiceProvider services = serviceCollection.BuildServiceProvider();
            var memoryCache = services.GetRequiredService<IMemoryCache>();
            var queryStorage = services.GetRequiredService<InMemoryQueryStorage>();

            const string queryId = "abc";
            DocumentNode query = Utf8GraphQLParser.Parse("{ __typename }");

            // act
            await queryStorage.WriteQueryAsync(
                queryId,
                new QueryDocument(query),
                CancellationToken.None);

            // assert
            Assert.True(memoryCache.TryGetValue(queryId, out object o));
            await Assert.IsType<Task<QueryDocument>>(o);
        }

         [Fact]
        public async Task Read_Query_From_Storage()
        {
            // arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddMemoryCache();
            serviceCollection.AddInMemoryQueryStorage();

            IServiceProvider services = serviceCollection.BuildServiceProvider();
            var memoryCache = services.GetRequiredService<IMemoryCache>();
            var queryStorage = services.GetRequiredService<InMemoryQueryStorage>();

            const string queryId = "abc";
            DocumentNode query = Utf8GraphQLParser.Parse("{ __typename }");
            await memoryCache.GetOrCreate(
                queryId, 
                item => Task.FromResult(new QueryDocument(query)));

            // act
            QueryDocument document = await queryStorage.TryReadQueryAsync(
                queryId,
                CancellationToken.None);

            // assert
            Assert.NotNull(document);
            Assert.Same(query, document.Document);
        }
    }
}
