using System.Collections.ObjectModel;
using System.Text;
using CookieCrumble;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HotChocolate.AspNetCore;

public class BatchPersistedQueryTests : ServerTestBase
{
    private const string persistedQuery = "persistedQuery";

    internal enum PersistenceStyle
    {
        Apollo,
        HotChocolate,
    }

    internal struct QueryInput
    {
        public  string OperationName;
        public  string Query;
    }
    public BatchPersistedQueryTests(TestServerFactory serverFactory) : base(serverFactory)
    {
    }

    private ClientQueryRequest CreatePersistedRequest( PersistenceStyle style,DocumentHashProviderBase hashProvider,string hash)
    {
       if (style == PersistenceStyle.Apollo)
        {
            return new ClientQueryRequest()
            {
                Extensions = new Dictionary<string, object?>
                {
                    ["persistedQuery"] = new Dictionary<string, object?>
                    {
                        ["version"] = 1, [hashProvider.Name] = hash
                    }
                }
            };
        }
        else if (style == PersistenceStyle.HotChocolate)
        {
           return new ClientQueryRequest()
            {
                Id = hash
            };
        }
        else
        {
            throw new NotImplementedException();
        }


    }
    private void ValidatePersisResult(ClientQueryResult? item,string hashProviderName ,string hash)
    {
        Assert.NotNull(item);
        var persistedExtensionResponse = item.Extensions?.FirstOrDefault(p => p.Key == persistedQuery).Value;
        Assert.NotNull(persistedExtensionResponse);
        var pjobject = (JObject)persistedExtensionResponse;
        var hashResponse = pjobject.Value<string>(hashProviderName);
        var persistedResponse = pjobject.Value<bool>("persisted");

        Assert.True(persistedResponse);
        Assert.Equal(hashResponse, hash);
    }

    private Dictionary<string, object?>? GeneratePersistedQueryExtension(string hashName, string hash)
    {
        return new Dictionary<string, object?>()
        {
            [ "persistedQuery"]= new Dictionary<string, object>() { [ "version"]= 1 ,  [hashName] = hash }
        };
    }


    private async Task<Tuple<IReadOnlyCollection<ClientQueryResult>, List<ClientQueryRequest>>> Persist(
        TestServer server,DocumentHashProviderBase hashProvider, PersistenceStyle style, params QueryInput[] queries)
    {
        var hashQueries =
            queries.Select(q => new { query = q.Query,operationName=q.OperationName, hash = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(q.Query)) });
        var enumerable = hashQueries.ToList();
        var requests = enumerable.Select(hq =>

                new ClientQueryRequest()
                {
                    OperationName = hq.operationName,
                    Extensions = GeneratePersistedQueryExtension(hashProvider.Name, hq.hash),
                    Query = hq.query,
                    Variables = new Dictionary<string, object?>()
                }


        ).ToList();

        var nonPersistedResults = await server.PostAsync(
            new ReadOnlyCollection<ClientQueryRequest>(requests),
            path: "/graphql");
        var i = 0;
        foreach (var nonPersistedResult in nonPersistedResults)
        {
            ValidatePersisResult(nonPersistedResult,hashProvider.Name, enumerable.ElementAt(i).hash);
            i++;
        }

        var batchParsistedQueries = requests.Select(r =>
        {
            var persisted = r.Extensions.FirstOrDefault(e => e.Key == persistedQuery);
            var castPersistedExtension = (string)(((Dictionary<string, object>)persisted.Value)
                .FirstOrDefault(d => d.Key == hashProvider.Name).Value);

            r = CreatePersistedRequest( style, hashProvider, castPersistedExtension);
            return r;
        }).ToList();
        return new(nonPersistedResults, batchParsistedQueries);
    }

    [Fact]
    public async Task HotChocolateStyle_MD5Hash_Success_Batch()
    {
        // arrange
        var storage = new QueryStorage();


        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL()
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
                );


        var query1 = new QueryInput()
        {
            Query = @"
                    query a {
                        a: hero {
                            name
                        }
                    }",
            OperationName = "a"
        };
        var query2 = new QueryInput()
        {
            Query = @"
                    query b {
                        b: hero {
                            name
                        }
                    }",
            OperationName = "b"
        };

        var hashProvider = new MD5DocumentHashProvider(HashFormat.Hex);
        var persistResponse = await Persist(server,hashProvider,PersistenceStyle.HotChocolate, query1, query2);
        var nonPersistedResults = persistResponse.Item1;
        var requests = persistResponse.Item2;

        // act
        var batchPersistedResult = await server.PostAsync(
            new ReadOnlyCollection<ClientQueryRequest>(requests),
            path: "/graphql");
        var i = 0;

        batchPersistedResult.ToList()
            .ForEach(pr =>
            {
                var nonPersistedResult = nonPersistedResults.ElementAt(i);
                var serialized1 = JsonConvert.SerializeObject(nonPersistedResult.Data);
                var serialized2 = JsonConvert.SerializeObject(pr.Data);
                Assert.Equal(serialized1, serialized2);
                i++;
            });

        // assert
        batchPersistedResult.MatchSnapshot();
    }

    [Fact]
    public async Task HotChocolateStyle_MD5Hash_NotFound_Batch()
    {
        // arrange
        var storage = new QueryStorage();


        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL()
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
                );

        var query1 = @"
                    query a {
                        a: hero {
                            name
                        }
                    }";
        var query2 = @"
                    query b {
                        b: hero {
                            name
                        }
                    }";
        var hashProvider = new MD5DocumentHashProvider(HashFormat.Hex);
        var key1 = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query1));
        var key2 = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query1));
        // we are not adding the query to the store so the server request should fail
        // storage.AddQuery(key, query);

        // act
        var result = await server.PostAsync(
            new ReadOnlyCollection<ClientQueryRequest>(new List<ClientQueryRequest>()
            {
                new() { Id = key1 }, new() { Id = key2 }
            }),
            path: "/graphql");

        // assert
        result.ToList().ForEach(r =>
        {
            Assert.NotNull(r.Errors);
            Assert.Null(r.Data);
        });
        result.MatchSnapshot();
    }

    [Fact]
    public async Task HotChocolateStyle_Sha1Hash_Success_Batch()
    {
        // arrange
        var storage = new QueryStorage();
        var hashProvider = new Sha1DocumentHashProvider(HashFormat.Hex);

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddSha1DocumentHashProvider(HashFormat.Hex)
                .AddGraphQL()
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
                );

        var query1 = new QueryInput()
        {
            Query = @"
                    query a {
                        a: hero {
                            name
                        }
                    }",
            OperationName = "a"
        };
        var query2 = new QueryInput()
        {
            Query = @"
                    query b {
                        b: hero {
                            name
                        }
                    }",
            OperationName = "b"
        };

        var persistResponse = await Persist(server,hashProvider,PersistenceStyle.HotChocolate, query1, query2);
        var nonPersistedResults = persistResponse.Item1;
        var requests = persistResponse.Item2;


        // act

        var batchPersistedResult = await server.PostAsync(
            new ReadOnlyCollection<ClientQueryRequest>(requests),
            path: "/graphql");
        // assert
        var i = 0;

        batchPersistedResult.ToList()
            .ForEach(pr =>
            {
                var nonPersistedResult = nonPersistedResults.ElementAt(i);
                var serialized1 = JsonConvert.SerializeObject(nonPersistedResult.Data);
                var serialized2 = JsonConvert.SerializeObject(pr.Data);
                Assert.Equal(serialized1, serialized2);
                i++;
            });
        batchPersistedResult.MatchSnapshot();
    }

    [Fact]
    public async Task HotChocolateStyle_Sha256Hash_Success_Batch()
    {
        // arrange
        var storage = new QueryStorage();
        var hashProvider = new Sha256DocumentHashProvider(HashFormat.Hex);

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddSha256DocumentHashProvider(HashFormat.Hex)
                .AddGraphQL()
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
                );

        var query1 = new QueryInput()
        {
            Query = @"
                    query a {
                        a: hero {
                            name
                        }
                    }",
            OperationName = "a"
        };
        var query2 = new QueryInput()
        {
            Query = @"
                    query b {
                        b: hero {
                            name
                        }
                    }",
            OperationName = "b"
        };
        var persistResponse = await Persist(server,hashProvider,PersistenceStyle.HotChocolate, query1, query2);
        var nonPersistedResults = persistResponse.Item1;
        var requests = persistResponse.Item2;
        // act

        var batchPersistedResult = await server.PostAsync(
            new ReadOnlyCollection<ClientQueryRequest>(requests),
            path: "/graphql");
        // assert
        var i = 0;

        batchPersistedResult.ToList()
            .ForEach(pr =>
            {
                var nonPersistedResult = nonPersistedResults.ElementAt(i);
                var serialized1 = JsonConvert.SerializeObject(nonPersistedResult.Data);
                var serialized2 = JsonConvert.SerializeObject(pr.Data);
                Assert.Equal(serialized1, serialized2);
                i++;
            });
        batchPersistedResult.MatchSnapshot();
    }


    [Fact]
    public async Task ApolloStyle_MD5Hash_Success_Batch()
    {
        // arrange
        var storage = new QueryStorage();
        var hashProvider = new MD5DocumentHashProvider(HashFormat.Hex);
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL()
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
                );

        var query1 = new QueryInput()
        {
            Query = @"
                    query a {
                        a: hero {
                            name
                        }
                    }",
            OperationName = "a"
        };
        var query2 = new QueryInput()
        {
            Query = @"
                    query b {
                        b: hero {
                            name
                        }
                    }",
            OperationName = "b"
        };
        var persistResponse = await Persist(server,hashProvider,PersistenceStyle.Apollo, query1, query2);
        var nonPersistedResults = persistResponse.Item1;
        var requests = persistResponse.Item2;

        // act
        var batchPersistedResult = await server.PostAsync(
            requests,
            path: "/graphql");

        // assert
        var i = 0;
        batchPersistedResult.ToList()
            .ForEach(pr =>
            {
                var nonPersistedResult = nonPersistedResults.ElementAt(i);
                var serialized1 = JsonConvert.SerializeObject(nonPersistedResult.Data);
                var serialized2 = JsonConvert.SerializeObject(pr.Data);
                Assert.Equal(serialized1, serialized2);
                i++;
            });
        batchPersistedResult.MatchSnapshot();
    }

    [Fact]
    public async Task ApolloStyle_MD5Hash_NotFound_Batch()
    {
        // arrange
        var storage = new QueryStorage();
        var hashProvider = new MD5DocumentHashProvider(HashFormat.Hex);

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL()
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
                );

        var query1 =  @"
                    query a {
                        a: hero {
                            name
                        }
                    }";
        var query2 = @"
                    query b {
                        b: hero {
                            name
                        }
                    }";
        var key1 = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query1));
        var key2 = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query1));

        // we are not adding the query to the store so the server request should fail
        // storage.AddQuery(key, query);

        // act

        var batchPersistedResult = await server.PostAsync(
            new ReadOnlyCollection<ClientQueryRequest>(new List<ClientQueryRequest>()
            {
                CreatePersistedRequest(PersistenceStyle.Apollo,hashProvider,key1),
                CreatePersistedRequest(PersistenceStyle.Apollo,hashProvider,key2)
            }),
            path: "/graphql");

        // assert
        var i = 0;

        batchPersistedResult.MatchSnapshot();
    }

    [Fact]
    public async Task ApolloStyle_Sha1Hash_Success_Batch()
    {
        // arrange
        var storage = new QueryStorage();
        var hashProvider = new Sha1DocumentHashProvider(HashFormat.Hex);

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddSha1DocumentHashProvider(HashFormat.Hex)
                .AddGraphQL()
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
                );

        var query1 = new QueryInput()
        {
            Query = @"
                    query a {
                        a: hero {
                            name
                        }
                    }",
            OperationName = "a"
        };
        var query2 = new QueryInput()
        {
            Query = @"
                    query b {
                        b: hero {
                            name
                        }
                    }",
            OperationName = "b"
        };
        var persistResponse = await Persist(server,hashProvider,PersistenceStyle.Apollo, query1, query2);
        var nonPersistedResults = persistResponse.Item1;
        var requests = persistResponse.Item2;

        // act
        var batchPersistedResult = await server.PostAsync(
            requests,
            path: "/graphql");


        // assert
        var i = 0;
        batchPersistedResult.ToList()
            .ForEach(pr =>
            {
                var nonPersistedResult = nonPersistedResults.ElementAt(i);
                var serialized1 = JsonConvert.SerializeObject(nonPersistedResult.Data);
                var serialized2 = JsonConvert.SerializeObject(pr.Data);
                Assert.Equal(serialized1, serialized2);
                i++;
            });
        batchPersistedResult.MatchSnapshot();
    }

    [Fact]
    public async Task ApolloStyle_Sha256Hash_Success_Batch()
    {
        // arrange
        var storage = new QueryStorage();
        var hashProvider = new Sha256DocumentHashProvider(HashFormat.Hex);

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddSha256DocumentHashProvider(HashFormat.Hex)
                .AddGraphQL()
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
                );

        var query1 = new QueryInput()
        {
            Query = @"
                    query a {
                        a: hero {
                            name
                        }
                    }",
            OperationName = "a"
        };
        var query2 = new QueryInput()
        {
            Query = @"
                    query b {
                        b: hero {
                            name
                        }
                    }",
            OperationName = "b"
        };
        var persistResponse = await Persist(server,hashProvider,PersistenceStyle.Apollo, query1, query2);
        var nonPersistedResults = persistResponse.Item1;
        var requests = persistResponse.Item2;
        // act
        var batchPersistedResult = await server.PostAsync(
            requests,
            path: "/graphql");
        // assert
        var i = 0;
        batchPersistedResult.ToList()
            .ForEach(pr =>
            {
                var nonPersistedResult = nonPersistedResults.ElementAt(i);
                var serialized1 = JsonConvert.SerializeObject(nonPersistedResult.Data);
                var serialized2 = JsonConvert.SerializeObject(pr.Data);
                Assert.Equal(serialized1, serialized2);
                i++;
            });
        batchPersistedResult.MatchSnapshot();
    }


    [Fact]
    public async Task Standard_Query_By_Default_Works_Batch()
    {
        // arrange
        var storage = new QueryStorage();

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL()
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
                );

        var query1 = @"
                    query a {
                        a: hero {
                            name
                        }
                    }";
        var query2 = @"
                    query b {
                        b: hero {
                            name
                        }
                    }";

        // act
        var result = await server.PostAsync(
            new ReadOnlyCollection<ClientQueryRequest>(new List<ClientQueryRequest>()
            {
                new() { Query = query1 }, new() { Query = query2 }
            }),
            path: "/graphql");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Standard_Query_Not_Allowed_Batch()
    {
        // arrange
        var storage = new QueryStorage();

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL()
                .ModifyRequestOptions(o => o.OnlyAllowPersistedQueries = true)
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
                .UsePersistedQueryPipeline());

        var query1 = @"
                    query a {
                        a: hero {
                            name
                        }
                    }";
        var query2 = @"
                    query b {
                        b: hero {
                            name
                        }
                    }";

        // act
        var result = await server.PostAsync(
            new ReadOnlyCollection<ClientQueryRequest>(new List<ClientQueryRequest>()
            {
                new() { Query = query1 }, new() { Query = query2 }
            }),
            path: "/graphql");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Standard_Query_Not_Allowed_Custom_Error_Batch()
    {
        // arrange
        var storage = new QueryStorage();

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL()
                .ModifyRequestOptions(o =>
                {
                    o.OnlyAllowPersistedQueries = true;
                    o.OnlyPersistedQueriesAreAllowedError =
                        ErrorBuilder.New()
                            .SetMessage("Not allowed!")
                            .Build();
                })
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
                .UsePersistedQueryPipeline()
                );

        var query1 = @"
                    query a {
                        a: hero {
                            name
                        }
                    }";
        var query2 = @"
                    query b {
                        b: hero {
                            name
                        }
                    }";


        // act
        var result = await server.PostAsync(
            new ReadOnlyCollection<ClientQueryRequest>(new List<ClientQueryRequest>()
            {
                new() { Query = query1 }, new() { Query = query2 }
            }),
            path: "/graphql");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Standard_Query_Not_Allowed_Override_Per_Request_Batch()
    {
        // arrange
        var storage = new QueryStorage();

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL()
                .ModifyRequestOptions(o =>
                {
                    o.OnlyAllowPersistedQueries = true;
                })
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))

                .AddHttpRequestInterceptor<AllowPersistedQueryInterceptor>());

        var query1 = @"
                    query a {
                        a: hero {
                            name
                        }
                    }";
        var query2 = @"
                    query b {
                        b: hero {
                            name
                        }
                    }";

        // act
        var result = await server.PostAsync(
            new ReadOnlyCollection<ClientQueryRequest>(new List<ClientQueryRequest>()
            {
                new() { Query = query1 }, new() { Query = query2 }
            }),
            path: "/graphql");

        // assert
        result.MatchSnapshot();
    }


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

    private sealed class AllowPersistedQueryInterceptor : DefaultHttpRequestInterceptor
    {
        public override ValueTask OnCreateAsync(
            HttpContext context,
            IRequestExecutor requestExecutor,
            IQueryRequestBuilder requestBuilder,
            CancellationToken cancellationToken)
        {
            requestBuilder.AllowNonPersistedQuery();
            return base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
        }
    }
}
