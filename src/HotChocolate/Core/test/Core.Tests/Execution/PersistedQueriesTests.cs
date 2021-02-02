using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution
{
    public class PersistedQueriesTests
    {
        [Fact]
        public async Task PersistedQueries_UnknownQuery()
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            // configure presistence
            serviceCollection.AddGraphQLSchema(b => b
                .AddDocumentFromString("type Query { foo: String }")
                .AddResolver("Query", "foo", "bar"));
            serviceCollection.AddQueryExecutor(b => b
                .AddSha256DocumentHashProvider()
                .UsePersistedQueryPipeline());

            // add in-memory query storage
            serviceCollection.AddSingleton<InMemoryQueryStorage>();
            serviceCollection.AddSingleton<IReadStoredQueries>(sp =>
                sp.GetRequiredService<InMemoryQueryStorage>());
            serviceCollection.AddSingleton<IWriteStoredQueries>(sp =>
                sp.GetRequiredService<InMemoryQueryStorage>());

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor =
                services.GetRequiredService<IQueryExecutor>();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQueryName("foo")
                    .Create());

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task PersistedQueries_NonPersistedQuery_IsExecuted()
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            // configure presistence
            serviceCollection.AddGraphQLSchema(b => b
                .AddDocumentFromString("type Query { foo: String }")
                .AddResolver("Query", "foo", "bar"));
            serviceCollection.AddQueryExecutor(b => b
                .AddSha256DocumentHashProvider()
                .UsePersistedQueryPipeline());

            // add in-memory query storage
            serviceCollection.AddSingleton<InMemoryQueryStorage>();
            serviceCollection.AddSingleton<IReadStoredQueries>(sp =>
                sp.GetRequiredService<InMemoryQueryStorage>());
            serviceCollection.AddSingleton<IWriteStoredQueries>(sp =>
                sp.GetRequiredService<InMemoryQueryStorage>());

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor =
                services.GetRequiredService<IQueryExecutor>();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .Create());

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task PersistedQueries_PersistedQuery_IsExecuted()
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            // configure presistence
            serviceCollection.AddGraphQLSchema(b => b
                .AddDocumentFromString("type Query { foo: String }")
                .AddResolver("Query", "foo", "bar"));
            serviceCollection.AddQueryExecutor(b => b
                .AddSha256DocumentHashProvider()
                .UsePersistedQueryPipeline());

            // add in-memory query storage
            serviceCollection.AddSingleton<InMemoryQueryStorage>();
            serviceCollection.AddSingleton<IReadStoredQueries>(sp =>
                sp.GetRequiredService<InMemoryQueryStorage>());
            serviceCollection.AddSingleton<IWriteStoredQueries>(sp =>
                sp.GetRequiredService<InMemoryQueryStorage>());

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor =
                services.GetRequiredService<IQueryExecutor>();

            var hashProvider =
                services.GetRequiredService<IDocumentHashProvider>();
            var storage = services.GetRequiredService<InMemoryQueryStorage>();

            var query = new QuerySourceText("{ foo }");
            string hash = hashProvider.ComputeHash(query.AsSpan());
            await storage.WriteQueryAsync(hash, query);

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQueryName(hash)
                    .Create());

            // assert
            Assert.True(storage.ReadWithSuccess);
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ActivePersistedQueries_UnknownQuery()
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            // configure presistence
            serviceCollection.AddGraphQLSchema(b => b
                .AddDocumentFromString("type Query { foo: String }")
                .AddResolver("Query", "foo", "bar"));
            serviceCollection.AddQueryExecutor(b => b
                .AddSha256DocumentHashProvider()
                .UseActivePersistedQueryPipeline());

            // add in-memory query storage
            serviceCollection.AddSingleton<InMemoryQueryStorage>();
            serviceCollection.AddSingleton<IReadStoredQueries>(sp =>
                sp.GetRequiredService<InMemoryQueryStorage>());
            serviceCollection.AddSingleton<IWriteStoredQueries>(sp =>
                sp.GetRequiredService<InMemoryQueryStorage>());

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor =
                services.GetRequiredService<IQueryExecutor>();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQueryName("foo")
                    .Create());

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ActivePersistedQueries_NonPersistedQuery_IsExecuted()
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            // configure presistence
            serviceCollection.AddGraphQLSchema(b => b
                .AddDocumentFromString("type Query { foo: String }")
                .AddResolver("Query", "foo", "bar"));
            serviceCollection.AddQueryExecutor(b => b
                .AddSha256DocumentHashProvider()
                .UseActivePersistedQueryPipeline());

            // add in-memory query storage
            serviceCollection.AddSingleton<InMemoryQueryStorage>();
            serviceCollection.AddSingleton<IReadStoredQueries>(sp =>
                sp.GetRequiredService<InMemoryQueryStorage>());
            serviceCollection.AddSingleton<IWriteStoredQueries>(sp =>
                sp.GetRequiredService<InMemoryQueryStorage>());

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor =
                services.GetRequiredService<IQueryExecutor>();

            var storage = services.GetRequiredService<InMemoryQueryStorage>();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .Create());

            // assert
            Assert.False(storage.WrittenQuery.HasValue);
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ActivePersistedQueries_PersistedQuery_IsExecuted()
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            // configure presistence
            serviceCollection.AddGraphQLSchema(b => b
                .AddDocumentFromString("type Query { foo: String }")
                .AddResolver("Query", "foo", "bar"));
            serviceCollection.AddQueryExecutor(b => b
                .AddSha256DocumentHashProvider()
                .UseActivePersistedQueryPipeline());

            // add in-memory query storage
            serviceCollection.AddSingleton<InMemoryQueryStorage>();
            serviceCollection.AddSingleton<IReadStoredQueries>(sp =>
                sp.GetRequiredService<InMemoryQueryStorage>());
            serviceCollection.AddSingleton<IWriteStoredQueries>(sp =>
                sp.GetRequiredService<InMemoryQueryStorage>());

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor =
                services.GetRequiredService<IQueryExecutor>();

            var hashProvider =
                services.GetRequiredService<IDocumentHashProvider>();
            var storage = services.GetRequiredService<InMemoryQueryStorage>();

            var query = new QuerySourceText("{ foo }");
            string hash = hashProvider.ComputeHash(query.AsSpan());
            await storage.WriteQueryAsync(hash, query);

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQueryName(hash)
                    .Create());

            // assert
            Assert.True(storage.ReadWithSuccess);
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ActivePersistedQueries_SaveQuery_And_Execute()
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            // configure presistence
            serviceCollection.AddGraphQLSchema(b => b
                .AddDocumentFromString("type Query { foo: String }")
                .AddResolver("Query", "foo", "bar"));
            serviceCollection.AddQueryExecutor(b => b
                .AddSha256DocumentHashProvider()
                .UseActivePersistedQueryPipeline());

            // add in-memory query storage
            serviceCollection.AddSingleton<InMemoryQueryStorage>();
            serviceCollection.AddSingleton<IReadStoredQueries>(sp =>
                sp.GetRequiredService<InMemoryQueryStorage>());
            serviceCollection.AddSingleton<IWriteStoredQueries>(sp =>
                sp.GetRequiredService<InMemoryQueryStorage>());

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor =
                services.GetRequiredService<IQueryExecutor>();

            var hashProvider =
                services.GetRequiredService<IDocumentHashProvider>();
            var storage = services.GetRequiredService<InMemoryQueryStorage>();

            var query = new QuerySourceText("{ foo }");
            string hash = hashProvider.ComputeHash(query.AsSpan());

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(query.Text)
                    .SetQueryName(hash)
                    .AddExtension("persistedQuery",
                        new Dictionary<string, object>
                        {
                            { "sha256Hash", hash }
                        })
                    .Create());

            // assert
            Assert.True(storage.WrittenQuery);
            result.ToJson().MatchSnapshot();
        }

        [InlineData(HashFormat.Base64)]
        [InlineData(HashFormat.Hex)]
        [Theory]
        public async Task ActivePersistedQueries_SaveQuery_InvalidHash_Sha1(
            HashFormat format)
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            // configure presistence
            serviceCollection.AddGraphQLSchema(b => b
                .AddDocumentFromString("type Query { foo: String }")
                .AddResolver("Query", "foo", "bar"));
            serviceCollection.AddQueryExecutor(b => b
                .AddSha1DocumentHashProvider(format)
                .UseActivePersistedQueryPipeline());

            // add in-memory query storage
            serviceCollection.AddSingleton<InMemoryQueryStorage>();
            serviceCollection.AddSingleton<IReadStoredQueries>(sp =>
                sp.GetRequiredService<InMemoryQueryStorage>());
            serviceCollection.AddSingleton<IWriteStoredQueries>(sp =>
                sp.GetRequiredService<InMemoryQueryStorage>());

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor =
                services.GetRequiredService<IQueryExecutor>();

            var hashProvider =
                services.GetRequiredService<IDocumentHashProvider>();
            var storage = services.GetRequiredService<InMemoryQueryStorage>();

            var query = new QuerySourceText("{ foo }");
            string hash = hashProvider.ComputeHash(query.AsSpan());

            // act and assert
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQueryName(hash)
                    .AddExtension("persistedQuery",
                        new Dictionary<string, object>
                        {
                            { hashProvider.Name, hash }
                        })
                    .Create());
            result.MatchSnapshot(new SnapshotNameExtension(
                "Query_Not_Found_" + format));

            result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQueryName(hash)
                    .SetQuery("{ foo }")
                    .AddExtension("persistedQuery",
                        new Dictionary<string, object>
                        {
                            { hashProvider.Name, hash }
                        })
                    .Create());
            result.MatchSnapshot(new SnapshotNameExtension(
                "Query_Stored_" + format));

            result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQueryName(hash)
                    .AddExtension("persistedQuery",
                        new Dictionary<string, object>
                        {
                            { hashProvider.Name, hash }
                        })
                    .Create());
            result.MatchSnapshot(new SnapshotNameExtension(
                "Query_Loaded_From_Cache_" + format));
        }

        [InlineData(HashFormat.Base64)]
        [InlineData(HashFormat.Hex)]
        [Theory]
        public async Task ActivePersistedQueries_SaveQuery_InvalidHash_Sha256(
            HashFormat format)
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            // configure presistence
            serviceCollection.AddGraphQLSchema(b => b
                .AddDocumentFromString("type Query { foo: String }")
                .AddResolver("Query", "foo", "bar"));
            serviceCollection.AddQueryExecutor(b => b
                .AddSha256DocumentHashProvider(format)
                .UseActivePersistedQueryPipeline());

            // add in-memory query storage
            serviceCollection.AddSingleton<InMemoryQueryStorage>();
            serviceCollection.AddSingleton<IReadStoredQueries>(sp =>
                sp.GetRequiredService<InMemoryQueryStorage>());
            serviceCollection.AddSingleton<IWriteStoredQueries>(sp =>
                sp.GetRequiredService<InMemoryQueryStorage>());

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor =
                services.GetRequiredService<IQueryExecutor>();

            var hashProvider =
                services.GetRequiredService<IDocumentHashProvider>();
            var storage = services.GetRequiredService<InMemoryQueryStorage>();

            var query = new QuerySourceText("{ foo }");
            string hash = hashProvider.ComputeHash(query.AsSpan());

            // act and assert
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQueryName(hash)
                    .AddExtension("persistedQuery",
                        new Dictionary<string, object>
                        {
                            { hashProvider.Name, hash }
                        })
                    .Create());
            result.MatchSnapshot(new SnapshotNameExtension(
                "Query_Not_Found_" + format));

            result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQueryName(hash)
                    .SetQuery("{ foo }")
                    .AddExtension("persistedQuery",
                        new Dictionary<string, object>
                        {
                            { hashProvider.Name, hash }
                        })
                    .Create());
            result.MatchSnapshot(new SnapshotNameExtension(
                "Query_Stored_" + format));

            result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQueryName(hash)
                    .AddExtension("persistedQuery",
                        new Dictionary<string, object>
                        {
                            { hashProvider.Name, hash }
                        })
                    .Create());
            result.MatchSnapshot(new SnapshotNameExtension(
                "Query_Loaded_From_Cache_" + format));
        }

        [InlineData(HashFormat.Base64)]
        [InlineData(HashFormat.Hex)]
        [Theory]
        public async Task ActivePersistedQueries_SaveQuery_InvalidHash_MD5(
            HashFormat format)
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            // configure presistence
            serviceCollection.AddGraphQLSchema(b => b
                .AddDocumentFromString("type Query { foo: String }")
                .AddResolver("Query", "foo", "bar"));
            serviceCollection.AddQueryExecutor(b => b
                .AddMD5DocumentHashProvider(format)
                .UseActivePersistedQueryPipeline());

            // add in-memory query storage
            serviceCollection.AddSingleton<InMemoryQueryStorage>();
            serviceCollection.AddSingleton<IReadStoredQueries>(sp =>
                sp.GetRequiredService<InMemoryQueryStorage>());
            serviceCollection.AddSingleton<IWriteStoredQueries>(sp =>
                sp.GetRequiredService<InMemoryQueryStorage>());

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor =
                services.GetRequiredService<IQueryExecutor>();

            var hashProvider =
                services.GetRequiredService<IDocumentHashProvider>();
            var storage = services.GetRequiredService<InMemoryQueryStorage>();

            var query = new QuerySourceText("{ foo }");
            string hash = hashProvider.ComputeHash(query.AsSpan());

            // act and assert
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQueryName(hash)
                    .AddExtension("persistedQuery",
                        new Dictionary<string, object>
                        {
                            { hashProvider.Name, hash }
                        })
                    .Create());
            result.MatchSnapshot(new SnapshotNameExtension(
                "Query_Not_Found_" + format));

            result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQueryName(hash)
                    .SetQuery("{ foo }")
                    .AddExtension("persistedQuery",
                        new Dictionary<string, object>
                        {
                            { hashProvider.Name, hash }
                        })
                    .Create());
            result.MatchSnapshot(new SnapshotNameExtension(
                "Query_Stored_" + format));

            result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQueryName(hash)
                    .AddExtension("persistedQuery",
                        new Dictionary<string, object>
                        {
                            { hashProvider.Name, hash }
                        })
                    .Create());
            result.MatchSnapshot(new SnapshotNameExtension(
                "Query_Loaded_From_Cache_" + format));
        }

        [Fact]
        public async Task ActivePersistedQueries_Invalid_Hash()
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            // configure presistence
            serviceCollection.AddGraphQLSchema(b => b
                .AddDocumentFromString("type Query { foo: String }")
                .AddResolver("Query", "foo", "bar"));
            serviceCollection.AddQueryExecutor(b => b
                .AddSha256DocumentHashProvider(HashFormat.Hex)
                .UseActivePersistedQueryPipeline());

            // add in-memory query storage
            serviceCollection.AddSingleton<InMemoryQueryStorage>();
            serviceCollection.AddSingleton<IReadStoredQueries>(sp =>
                sp.GetRequiredService<InMemoryQueryStorage>());
            serviceCollection.AddSingleton<IWriteStoredQueries>(sp =>
                sp.GetRequiredService<InMemoryQueryStorage>());

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor =
                services.GetRequiredService<IQueryExecutor>();

            IDocumentHashProvider hashProvider =
                services.GetRequiredService<IDocumentHashProvider>();
            InMemoryQueryStorage storage = services.GetRequiredService<InMemoryQueryStorage>();

            var query = new QuerySourceText("{ foo }");
            var hash = hashProvider.ComputeHash(query.AsSpan());

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(query.Text)
                    .SetQueryName(hash)
                    .AddExtension("persistedQuery",
                        new Dictionary<string, object>
                        {
                            { "sha256Hash", "123" }
                        })
                    .Create());

            // assert
            Assert.False(storage.WrittenQuery.HasValue);
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ActivePersistedQueries_Invalid_Hash_Type()
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            // configure presistence
            serviceCollection.AddGraphQLSchema(b => b
                .AddDocumentFromString("type Query { foo: String }")
                .AddResolver("Query", "foo", "bar"));
            serviceCollection.AddQueryExecutor(b => b
                .AddSha256DocumentHashProvider(HashFormat.Hex)
                .UseActivePersistedQueryPipeline());

            // add in-memory query storage
            serviceCollection.AddSingleton<InMemoryQueryStorage>();
            serviceCollection.AddSingleton<IReadStoredQueries>(sp =>
                sp.GetRequiredService<InMemoryQueryStorage>());
            serviceCollection.AddSingleton<IWriteStoredQueries>(sp =>
                sp.GetRequiredService<InMemoryQueryStorage>());

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor =
                services.GetRequiredService<IQueryExecutor>();

            var hashProvider =
                services.GetRequiredService<IDocumentHashProvider>();
            var storage = services.GetRequiredService<InMemoryQueryStorage>();

            var query = new QuerySourceText("{ foo }");
            string hash = hashProvider.ComputeHash(query.AsSpan());

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(query.Text)
                    .SetQueryName(hash)
                    .AddExtension("persistedQuery",
                        new Dictionary<string, object>
                        {
                            { "sha1Hash", "123" }
                        })
                    .Create());

            // assert
            Assert.False(storage.WrittenQuery.HasValue);
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ActivePersistedQueries_No_PersistedQuery_Section_In_Request()
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            // configure presistence
            serviceCollection.AddGraphQLSchema(b => b
                .AddDocumentFromString("type Query { foo: String }")
                .AddResolver("Query", "foo", "bar"));
            serviceCollection.AddQueryExecutor(b => b
                .AddSha256DocumentHashProvider(HashFormat.Hex)
                .UseActivePersistedQueryPipeline());

            // add in-memory query storage
            serviceCollection.AddSingleton<InMemoryQueryStorage>();
            serviceCollection.AddSingleton<IReadStoredQueries>(sp =>
                sp.GetRequiredService<InMemoryQueryStorage>());
            serviceCollection.AddSingleton<IWriteStoredQueries>(sp =>
                sp.GetRequiredService<InMemoryQueryStorage>());

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor =
                services.GetRequiredService<IQueryExecutor>();

            var hashProvider =
                services.GetRequiredService<IDocumentHashProvider>();
            var storage = services.GetRequiredService<InMemoryQueryStorage>();

            var query = new QuerySourceText("{ foo }");
            string hash = hashProvider.ComputeHash(query.AsSpan());

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(query.Text)
                    .SetQueryName(hash)
                    .Create());

            // assert
            Assert.False(storage.WrittenQuery.HasValue);
            result.ToJson().MatchSnapshot();
        }

        public class InMemoryQueryStorage
            : IReadStoredQueries
            , IWriteStoredQueries
        {
            private readonly Dictionary<string, byte[]> _store =
                new Dictionary<string, byte[]>();

            public bool? ReadWithSuccess { get; private set; }

            public bool? WrittenQuery { get; private set; }

            public Task<QueryDocument> TryReadQueryAsync(string queryId) =>
                TryReadQueryAsync(queryId, CancellationToken.None);

            public Task<QueryDocument> TryReadQueryAsync(
                string queryId,
                CancellationToken cancellationToken)
            {
                if (_store.TryGetValue(queryId, out byte[] value))
                {
                    ReadWithSuccess = true;
                    DocumentNode document = Utf8GraphQLParser.Parse(value);
                    return Task.FromResult<QueryDocument>(
                        new QueryDocument(document));
                }

                ReadWithSuccess = null;
                return Task.FromResult<QueryDocument>(null);
            }

            public Task WriteQueryAsync(string queryId, IQuery query) =>
                WriteQueryAsync(queryId, query, CancellationToken.None);

            public Task WriteQueryAsync(
                string queryId,
                IQuery query,
                CancellationToken cancellationToken)
            {
                _store[queryId] = query.AsSpan().ToArray();
                WrittenQuery = true;
                return Task.CompletedTask;
            }
        }
    }
}
