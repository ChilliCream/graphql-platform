using System.Text;
using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore;

public class PersistedQueryTests : ServerTestBase
{
    public PersistedQueryTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    [Fact]
    public async Task HotChocolateStyle_MD5Hash_Success()
    {
        // arrange
        var storage = new QueryStorage();
        var hashProvider = new MD5DocumentHashProvider(HashFormat.Hex);

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL("StarWars")
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
                .UsePersistedQueryPipeline());

        var query = "{ __typename }";
        var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
        storage.AddQuery(key, query);

        // act
        var result = await server.PostAsync(
            new ClientQueryRequest { Id = key, },
            path: "/starwars");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task HotChocolateStyle_MD5Hash_NotFound()
    {
        // arrange
        var storage = new QueryStorage();
        var hashProvider = new MD5DocumentHashProvider(HashFormat.Hex);

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL("StarWars")
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
                .UsePersistedQueryPipeline());

        var query = "{ __typename }";
        var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
        // we are not adding the query to the store so the server request should fail
        // storage.AddQuery(key, query);

        // act
        var result = await server.PostAsync(
            new ClientQueryRequest { Id = key, },
            path: "/starwars");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task HotChocolateStyle_Sha1Hash_Success()
    {
        // arrange
        var storage = new QueryStorage();
        var hashProvider = new Sha1DocumentHashProvider(HashFormat.Hex);

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddSha1DocumentHashProvider(HashFormat.Hex)
                .AddGraphQL("StarWars")
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
                .UsePersistedQueryPipeline());

        var query = "{ __typename }";
        var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
        storage.AddQuery(key, query);

        // act
        var result = await server.PostAsync(
            new ClientQueryRequest { Id = key, },
            path: "/starwars");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task HotChocolateStyle_Sha256Hash_Success()
    {
        // arrange
        var storage = new QueryStorage();
        var hashProvider = new Sha256DocumentHashProvider(HashFormat.Hex);

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddSha256DocumentHashProvider(HashFormat.Hex)
                .AddGraphQL("StarWars")
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
                .UsePersistedQueryPipeline());

        var query = "{ __typename }";
        var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
        storage.AddQuery(key, query);

        // act
        var result = await server.PostAsync(
            new ClientQueryRequest { Id = key, },
            path: "/starwars");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ApolloStyle_MD5Hash_Success()
    {
        // arrange
        var storage = new QueryStorage();
        var hashProvider = new MD5DocumentHashProvider(HashFormat.Hex);

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL("StarWars")
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
                .UsePersistedQueryPipeline());

        var query = "{ __typename }";
        var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
        storage.AddQuery(key, query);

        // act
        var result = await server.PostAsync(
            CreateApolloStyleRequest(hashProvider.Name, key),
            path: "/starwars");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ApolloStyle_MD5Hash_NotFound()
    {
        // arrange
        var storage = new QueryStorage();
        var hashProvider = new MD5DocumentHashProvider(HashFormat.Hex);

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL("StarWars")
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
                .UsePersistedQueryPipeline());

        var query = "{ __typename }";
        var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
        // we are not adding the query to the store so the server request should fail
        // storage.AddQuery(key, query);

        // act
        var result = await server.PostAsync(
            CreateApolloStyleRequest(hashProvider.Name, key),
            path: "/starwars");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ApolloStyle_Sha1Hash_Success()
    {
        // arrange
        var storage = new QueryStorage();
        var hashProvider = new Sha1DocumentHashProvider(HashFormat.Hex);

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddSha1DocumentHashProvider(HashFormat.Hex)
                .AddGraphQL("StarWars")
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
                .UsePersistedQueryPipeline());

        var query = "{ __typename }";
        var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
        storage.AddQuery(key, query);

        // act
        var result = await server.PostAsync(
            CreateApolloStyleRequest(hashProvider.Name, key),
            path: "/starwars");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ApolloStyle_Sha256Hash_Success()
    {
        // arrange
        var storage = new QueryStorage();
        var hashProvider = new Sha256DocumentHashProvider(HashFormat.Hex);

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddSha256DocumentHashProvider(HashFormat.Hex)
                .AddGraphQL("StarWars")
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
                .UsePersistedQueryPipeline());

        var query = "{ __typename }";
        var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
        storage.AddQuery(key, query);

        // act
        var result = await server.PostAsync(
            CreateApolloStyleRequest(hashProvider.Name, key),
            path: "/starwars");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Standard_Query_By_Default_Works()
    {
        // arrange
        var storage = new QueryStorage();

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL("StarWars")
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
                .UsePersistedQueryPipeline());

        var query = "{ __typename }";

        // act
        var result = await server.PostAsync(
            new ClientQueryRequest { Query = query, },
            path: "/starwars");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Standard_Query_Not_Allowed()
    {
        // arrange
        var storage = new QueryStorage();

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL("StarWars")
                .ModifyRequestOptions(o => o.OnlyAllowPersistedQueries = true)
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
                .UsePersistedQueryPipeline());

        var query = "{ __typename }";

        // act
        var result = await server.PostAsync(
            new ClientQueryRequest { Query = query, },
            path: "/starwars");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Standard_Query_Not_Allowed_Custom_Error()
    {
        // arrange
        var storage = new QueryStorage();

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL("StarWars")
                .ModifyRequestOptions(o =>
                {
                    o.OnlyAllowPersistedQueries = true;
                    o.OnlyPersistedQueriesAreAllowedError =
                        ErrorBuilder.New()
                            .SetMessage("Not allowed!")
                            .Build();
                })
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
                .UsePersistedQueryPipeline());

        var query = "{ __typename }";

        // act
        var result = await server.PostAsync(
            new ClientQueryRequest { Query = query, },
            path: "/starwars");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Standard_Query_Not_Allowed_Override_Per_Request()
    {
        // arrange
        var storage = new QueryStorage();

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL("StarWars")
                .ModifyRequestOptions(o =>
                {
                    o.OnlyAllowPersistedQueries = true;
                })
                .ConfigureSchemaServices(c => c.AddSingleton<IReadStoredQueries>(storage))
                .UsePersistedQueryPipeline()
                .AddHttpRequestInterceptor<AllowPersistedQueryInterceptor>());

        var query = "{ __typename }";

        // act
        var result = await server.PostAsync(
            new ClientQueryRequest { Query = query, },
            path: "/starwars");

        // assert
        result.MatchSnapshot();
    }

    private ClientQueryRequest CreateApolloStyleRequest(string hashName, string key)
        =>  new()
        {
            Extensions = new Dictionary<string, object?>
            {
                ["persistedQuery"] = new Dictionary<string, object?>
                {
                    ["version"] = 1,
                    [hashName] = key,
                },
            },
        };

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
