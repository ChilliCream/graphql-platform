using System.Collections.ObjectModel;
using System.Text;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.StarWars;
using HotChocolate.Tests;
using HotChocolate.Types;
using Snapshooter.Xunit;
using static HotChocolate.Tests.TestHelper;

namespace HotChocolate.Execution.Batching;

public class BatchPersistedExecutorTest
{
    private const string _persistedQuery = "persistedQuery";
    private const string _sha256Hash = "sha256Hash";
    private const string __version = "version";

    private sealed class QueryStorage : IReadStoredQueries
    {
        private readonly Dictionary<string, Task<QueryDocument?>> _cache =
            new(StringComparer.Ordinal);

        public Task<QueryDocument?> TryReadQueryAsync(
            string queryId,
            CancellationToken cancellationToken = default)
            => _cache.TryGetValue(queryId, out var value)
                ? value
                : Task.FromResult<QueryDocument?>(null);

        public void AddQuery(string key, string query)
        {
            var doc = new QueryDocument(Utf8GraphQLParser.Parse(query));
            _cache.Add(key, Task.FromResult<QueryDocument?>(doc));
        }
    }


    private static async Task<IEnumerable<IQueryRequest>> PersistAndGetAsync(QueryStorage storage,
        params string[] queries)
    {
        var hashProvider = new MD5DocumentHashProvider(HashFormat.Hex);
        return queries.Select(query =>
        {
            var hash = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
            storage.AddQuery(hash, query);
            return new QueryRequest(
                queryId: hash,
                queryHash: hash,
                extensions: new Dictionary<string, object>()
                {
                    {
                        _persistedQuery, new Dictionary<string, object>() { { _sha256Hash, hash }, { __version, 1 } }
                    }
                });
        });
    }


    [Fact]
    public async Task ExecuteExportScalar_PersistedBatch()
    {
        var storage = new QueryStorage();

        // arrange
        Snapshot.FullName();
        var executor = await CreateExecutorAsync(c =>
            c.AddStarWarsTypes()
                .AddExportDirectiveType()
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
                .UsePersistedQueryPipeline()
                .Services
                .AddStarWarsRepositories()
        );
        var query1 = @"
                        query getHero {
                            hero(episode: EMPIRE) {
                                id @export
                            }
                        }";
        var query2 = @"
                        query getHuman {
                            human(id: $id) {
                                name
                            }
                        }";

        // act
        var persistedBatch = await PersistAndGetAsync(storage, query1, query2);

        var batchResult = await executor.ExecuteBatchAsync(persistedBatch.ToList());


        // assert
        await batchResult.MatchSnapshotAsync();
    }

    [Fact]
    public async Task ExecuteExportScalarList_PersistedBatch()
    {
        // arrange
        var storage = new QueryStorage();
        Snapshot.FullName();
        var executor = await CreateExecutorAsync(c =>
            c
                .AddStarWarsTypes()
                .AddExportDirectiveType()
                .UsePersistedQueryPipeline()
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
                .Services
                .AddStarWarsRepositories()
        );
        var query1 = @"
                        query getHero {
                            hero(episode: EMPIRE) {
                                friends {
                                    nodes {
                                        id @export(as: ""abc"")
                                    }
                                }
                            }
                        }";
        var query2 = @"
                        query getCharacter {
                            character(characterIds: $abc) {
                                name
                            }
                        }";

        var persistedBatch = await PersistAndGetAsync(storage, query1, query2);

        var batchResult = await executor.ExecuteBatchAsync(persistedBatch.ToList());

        // assert
        await batchResult.MatchSnapshotAsync();
    }

    [Fact]
    public async Task ExecuteExportScalarList_ExplicitVariable_PersistedBatch()
    {
        // arrange
        var storage = new QueryStorage();
        Snapshot.FullName();
        var executor = await CreateExecutorAsync(c =>
            c
                .AddStarWarsTypes()
                .AddExportDirectiveType()
                .UsePersistedQueryPipeline()
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
                .Services
                .AddStarWarsRepositories()
        );

        var query1 = @"
                        query getHero {
                            hero(episode: EMPIRE) {
                                friends {
                                    nodes {
                                        id @export(as: ""abc"")
                                    }
                                }
                            }
                        }";
        var query2 = @"
                        query getCharacter($abc: [String!]!) {
                            character(characterIds: $abc) {
                                name
                            }
                        }";

        var persistedBatch = await PersistAndGetAsync(storage, query1, query2);
        var batchResult = await executor.ExecuteBatchAsync(persistedBatch.ToList());

        // assert
        await batchResult.MatchSnapshotAsync();
    }

    [Fact]
    public async Task ExecuteExportObject_PersistedBatch()
    {
        // arrange
        var storage = new QueryStorage();
        Snapshot.FullName();

        var executor = await CreateExecutorAsync(c =>
            c.AddStarWarsTypes()
                .AddExportDirectiveType()
                .AddInMemorySubscriptions()
                .UsePersistedQueryPipeline()
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
                .AddStarWarsRepositories());

        // act
        var query1 = @"mutation firstReview {
                            createReview(
                                episode: NEW_HOPE
                                review: { commentary: ""foo"", stars: 4 })
                                    @export(as: ""r"") {
                                commentary
                                stars
                            }
                        }";
        var query2 = @"
                        mutation secondReview {
                            createReview(
                                episode: EMPIRE
                                review: $r) {
                                commentary
                                stars
                            }
                        }";

        var persistedBatch = await PersistAndGetAsync(storage, query1, query2);

        var batchResult = await executor.ExecuteBatchAsync(persistedBatch.ToList());

        // assert
        await batchResult.MatchSnapshotAsync();
    }

    [Fact]
    public async Task ExecuteExportLeafList_PersistedBatch()
    {
        // arrange
        var storage = new QueryStorage();
        Snapshot.FullName();

        var executor = await CreateExecutorAsync(c =>
            c
                .AddQueryType(d => d.Name("Query")
                    .Field("foo")
                    .Argument("bar", a => a.Type<ListType<StringType>>())
                    .Type<ListType<StringType>>()
                    .Resolve(ctx =>
                    {
                        var list = ctx.ArgumentValue<List<string>>("bar");

                        if (list is null)
                        {
                            return new List<string> { "123", "456" };
                        }

                        list.Add("789");
                        return list;
                    }))
                .AddExportDirectiveType()
                .UsePersistedQueryPipeline()
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
        );

        // act
        var query1 = @"{
                            foo @export(as: ""b"")
                        }";
        var query2 = @"{
                            foo(bar: $b)
                        }";

        var persistedBatch = await PersistAndGetAsync(storage, query1, query2);
        var batchResult = await executor.ExecuteBatchAsync(persistedBatch.ToList());

        // assert
        await batchResult.MatchSnapshotAsync();
    }

    [Fact]
    public async Task ExecuteExportObjectList_PersistedBatch()
    {
        // arrange
        var storage = new QueryStorage();
        Snapshot.FullName();

        var executor = await CreateExecutorAsync(c =>
            c
                .AddDocumentFromString(
                    @"
                    type Query {
                        foo(f: [FooInput]) : [Foo]
                    }

                    type Foo {
                        bar: String!
                    }

                    input FooInput {
                        bar: String!
                    }")
                .AddResolver("Query", "foo", ctx =>
                {
                    var list = ctx.ArgumentValue<List<object>>("f");

                    if (list is null)
                    {
                        return new List<object> { new Dictionary<string, object> { { "bar", "123" } } };
                    }

                    list.Add(new Dictionary<string, object> { { "bar", "456" } });
                    return list;
                })
                .UseField(next => context =>
                {
                    var o = context.Parent<object>();
                    if (o is Dictionary<string, object> d
                        && d.TryGetValue(context.ResponseName, out var v))
                    {
                        context.Result = v;
                    }

                    return next(context);
                })
                .AddExportDirectiveType()
                .UsePersistedQueryPipeline()
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
        );

        // act
        var query1 = @"{
                            foo @export(as: ""b"")
                            {
                                bar
                            }
                        }";
        var query2 = @"{
                            foo(f: $b)
                            {
                                bar
                            }
                        }";

        var persistedBatch = await PersistAndGetAsync(storage, query1, query2);
        var batchResult = await executor.ExecuteBatchAsync(persistedBatch.ToList());

        // assert
        await batchResult.MatchSnapshotAsync();
    }

    [Fact]
    public async Task Add_Value_To_Variable_List_PersistedBatch()
    {
        // arrange
        var storage = new QueryStorage();
        Snapshot.FullName();

        var executor = await CreateExecutorAsync(c =>
            c
                .AddQueryType(d => d.Name("Query")
                    .Field("foo")
                    .Argument("bar", a => a.Type<ListType<StringType>>())
                    .Type<ListType<StringType>>()
                    .Resolve(ctx =>
                    {
                        var list = ctx.ArgumentValue<List<string>>("bar");
                        list.Add("789");
                        return list;
                    }))
                .AddExportDirectiveType()
                .UsePersistedQueryPipeline()
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
        );
        var hashProvider = new MD5DocumentHashProvider(HashFormat.Hex);
        // act
        var query1 = @"query foo1($b: [String]) {
                            foo(bar: $b) @export(as: ""b"")
                        }"
            ;
        var query2 = @"query foo2($b: [String]) {
                            foo(bar: $b)
                        }"
            ;
        var hash1 = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query1));
        var hash2 = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query2));
        storage.AddQuery(hash1, query1);
        storage.AddQuery(hash2, query2);
        var persistedBatch = new List<QueryRequest>()
        {
            new(
                queryId: hash1,
                queryHash: hash1,
                variableValues: new ReadOnlyDictionary<string, object>(
                    new Dictionary<string, object>() { { "b", new[] { "123" } } }),
                extensions: new Dictionary<string, object>()
                {
                    {
                        _persistedQuery, new Dictionary<string, object>() { { _sha256Hash, hash1 }, { __version, 1 } }
                    }
                }),
            new(
                queryId: hash2,
                queryHash: hash2,
                extensions: new Dictionary<string, object>()
                {
                    {
                        _persistedQuery, new Dictionary<string, object>() { { _sha256Hash, hash2 }, { __version, 1 } }
                    }
                })
        };

        var batchResult = await executor.ExecuteBatchAsync(persistedBatch.ToList());

        // assert
        await batchResult.MatchSnapshotAsync();
    }

    [Fact]
    public async Task Convert_List_To_Single_Value_With_Converters_PersistedBatch()
    {
        // arrange
        var storage = new QueryStorage();
        Snapshot.FullName();

        var executor = await CreateExecutorAsync(c => c
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("foo")
                    .Argument("bar", a => a.Type<ListType<StringType>>())
                    .Type<ListType<StringType>>()
                    .Resolve(ctx =>
                    {
                        var list = ctx.ArgumentValue<List<string>>("bar");
                        list.Add("789");
                        return list;
                    });

                d.Field("baz")
                    .Argument("bar", a => a.Type<StringType>())
                    .Resolve(ctx => ctx.ArgumentValue<string>("bar"));
            })
            .AddExportDirectiveType().UsePersistedQueryPipeline()
            .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
        );
        var hashProvider = new MD5DocumentHashProvider(HashFormat.Hex);
        // act

        var query1 = @"query foo1($b1: [String]) {
                            foo(bar: $b1) @export(as: ""b2"")
                        }";
        var query2 = @"query foo2($b2: String) {
                            baz(bar: $b2)
                        }";
        var hash1 = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query1));
        var hash2 = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query2));
        storage.AddQuery(hash1, query1);
        storage.AddQuery(hash2, query2);
        var persistedBatch = new List<QueryRequest>()
        {
            new(
                queryId: hash1,
                queryHash: hash1,
                variableValues: new ReadOnlyDictionary<string, object>(
                    new Dictionary<string, object>() { { "b1", new[] { "123" } } }),
                extensions: new Dictionary<string, object>()
                {
                    {
                        _persistedQuery, new Dictionary<string, object>() { { _sha256Hash, hash1 }, { __version, 1 } }
                    }
                }),
            new(
                queryId: hash2,
                queryHash: hash2,
                extensions: new Dictionary<string, object>()
                {
                    {
                        _persistedQuery, new Dictionary<string, object>() { { _sha256Hash, hash2 }, { __version, 1 } }
                    }
                })
        };
        var batchResult = await executor.ExecuteBatchAsync(persistedBatch.ToList());

        // assert
        await batchResult.MatchSnapshotAsync();
    }
}
