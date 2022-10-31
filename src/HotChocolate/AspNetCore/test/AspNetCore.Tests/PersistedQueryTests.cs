using System.Net;
using System.Security.Cryptography;
using System.Text;
using CookieCrumble;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Execution;
using HotChocolate.Language;
using Newtonsoft.Json;

namespace HotChocolate.AspNetCore;

public class PersistedQueryTests : ServerTestBase
{
    public PersistedQueryTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
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

    private ClientQueryRequest CreateApolloStyleRequest(string hashName, string key)
        =>  new()
        {
            Extensions = new Dictionary<string, object?>
            {
                ["persistedQuery"] = new Dictionary<string, object?>
                {
                    ["version"] = 1,
                    [hashName] = key
                }
            }
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
}
