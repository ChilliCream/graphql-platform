using System.Text;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using Microsoft.AspNetCore.Http;
using HotChocolate.PersistedOperations;

namespace HotChocolate.AspNetCore;

public class PersistedOperationTests(TestServerFactory serverFactory)
    : ServerTestBase(serverFactory)
{
    [Fact]
    public async Task HotChocolateStyle_MD5Hash_Success()
    {
        // arrange
        var storage = new OperationStorage();
        var hashProvider = new MD5DocumentHashProvider(HashFormat.Hex);

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL("StarWars")
                .ConfigureSchemaServices(c => c.AddSingleton<IOperationDocumentStorage>(storage))
                .UsePersistedOperationPipeline());

        const string query = "{ __typename }";
        var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
        storage.AddOperation(key.Value, query);

        // act
        var result = await server.PostAsync(
            new ClientQueryRequest { Id = key.Value },
            path: "/starwars");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task HotChocolateStyle_MD5Hash_NotFound()
    {
        // arrange
        var storage = new OperationStorage();
        var hashProvider = new MD5DocumentHashProvider(HashFormat.Hex);

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL("StarWars")
                .ConfigureSchemaServices(c => c.AddSingleton<IOperationDocumentStorage>(storage))
                .UsePersistedOperationPipeline());

        const string query = "{ __typename }";
        var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
        // we are not adding the query to the store so the server request should fail
        // storage.AddOperation(key, query);

        // act
        var result = await server.PostAsync(
            new ClientQueryRequest { Id = key.Value },
            path: "/starwars");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task HotChocolateStyle_Sha1Hash_Success()
    {
        // arrange
        var storage = new OperationStorage();
        var hashProvider = new Sha1DocumentHashProvider(HashFormat.Hex);

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL("StarWars")
                .AddSha1DocumentHashProvider(HashFormat.Hex)
                .ConfigureSchemaServices(c => c.AddSingleton<IOperationDocumentStorage>(storage))
                .UsePersistedOperationPipeline());

        const string query = "{ __typename }";
        var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
        storage.AddOperation(key.Value, query);

        // act
        var result = await server.PostAsync(
            new ClientQueryRequest { Id = key.Value },
            path: "/starwars");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task HotChocolateStyle_Sha256Hash_Success()
    {
        // arrange
        var storage = new OperationStorage();
        var hashProvider = new Sha256DocumentHashProvider(HashFormat.Hex);

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL("StarWars")
                .AddSha256DocumentHashProvider(HashFormat.Hex)
                .ConfigureSchemaServices(c => c.AddSingleton<IOperationDocumentStorage>(storage))
                .UsePersistedOperationPipeline());

        const string query = "{ __typename }";
        var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
        storage.AddOperation(key.Value, query);

        // act
        var result = await server.PostAsync(
            new ClientQueryRequest { Id = key.Value },
            path: "/starwars");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task HotChocolateStyle_Sha256Hash_Query_Empty_String_Success()
    {
        // arrange
        var storage = new OperationStorage();
        var hashProvider = new Sha256DocumentHashProvider(HashFormat.Hex);

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL("StarWars")
                .AddSha256DocumentHashProvider(HashFormat.Hex)
                .ConfigureSchemaServices(c => c.AddSingleton<IOperationDocumentStorage>(storage))
                .UsePersistedOperationPipeline());

        const string query = "{ __typename }";
        var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
        storage.AddOperation(key.Value, query);

        // act
        var result = await server.PostAsync(
            new ClientQueryRequest
            {
                Id = key.Value,
                Query = string.Empty
            },
            path: "/starwars");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ApolloStyle_MD5Hash_Success()
    {
        // arrange
        var storage = new OperationStorage();
        var hashProvider = new MD5DocumentHashProvider(HashFormat.Hex);

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL("StarWars")
                .ConfigureSchemaServices(c => c.AddSingleton<IOperationDocumentStorage>(storage))
                .UsePersistedOperationPipeline());

        const string query = "{ __typename }";
        var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
        storage.AddOperation(key.Value, query);

        // act
        var result = await server.PostAsync(
            CreateApolloStyleRequest(hashProvider.Name, key.Value),
            path: "/starwars");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ApolloStyle_MD5Hash_NotFound()
    {
        // arrange
        var storage = new OperationStorage();
        var hashProvider = new MD5DocumentHashProvider(HashFormat.Hex);

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL("StarWars")
                .ConfigureSchemaServices(c => c.AddSingleton<IOperationDocumentStorage>(storage))
                .UsePersistedOperationPipeline());

        const string query = "{ __typename }";
        var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
        // we are not adding the query to the store so the server request should fail
        // storage.AddOperation(key, query);

        // act
        var result = await server.PostAsync(
            CreateApolloStyleRequest(hashProvider.Name, key.Value),
            path: "/starwars");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ApolloStyle_Sha1Hash_Success()
    {
        // arrange
        var storage = new OperationStorage();
        var hashProvider = new Sha1DocumentHashProvider(HashFormat.Hex);

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL("StarWars")
                .AddSha1DocumentHashProvider(HashFormat.Hex)
                .ConfigureSchemaServices(c => c.AddSingleton<IOperationDocumentStorage>(storage))
                .UsePersistedOperationPipeline());

        const string query = "{ __typename }";
        var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
        storage.AddOperation(key.Value, query);

        // act
        var result = await server.PostAsync(
            CreateApolloStyleRequest(hashProvider.Name, key.Value),
            path: "/starwars");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ApolloStyle_Sha256Hash_Success()
    {
        // arrange
        var storage = new OperationStorage();
        var hashProvider = new Sha256DocumentHashProvider(HashFormat.Hex);

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL("StarWars")
                .AddSha256DocumentHashProvider(HashFormat.Hex)
                .ConfigureSchemaServices(c => c.AddSingleton<IOperationDocumentStorage>(storage))
                .UsePersistedOperationPipeline());

        const string query = "{ __typename }";
        var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
        storage.AddOperation(key.Value, query);

        // act
        var result = await server.PostAsync(
            CreateApolloStyleRequest(hashProvider.Name, key.Value),
            path: "/starwars");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Standard_Query_By_Default_Works()
    {
        // arrange
        var storage = new OperationStorage();

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL("StarWars")
                .ConfigureSchemaServices(c => c.AddSingleton<IOperationDocumentStorage>(storage))
                .UsePersistedOperationPipeline());

        const string query = "{ __typename }";

        // act
        var result = await server.PostAsync(
            new ClientQueryRequest { Query = query },
            path: "/starwars");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Standard_Query_Not_Allowed()
    {
        // arrange
        var storage = new OperationStorage();

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL("StarWars")
                .ModifyRequestOptions(o => o.PersistedOperations.OnlyAllowPersistedDocuments = true)
                .ConfigureSchemaServices(c => c.AddSingleton<IOperationDocumentStorage>(storage))
                .UsePersistedOperationPipeline());

        const string query = "{ __typename }";

        // act
        var result = await server.PostAsync(
            new ClientQueryRequest { Query = query },
            path: "/starwars");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Standard_Query_Not_Allowed_Even_When_Persisted()
    {
        // arrange
        var storage = new OperationStorage();
        storage.AddOperation(
            "a73defcdf38e5891e91b9ba532cf4c36",
            "query GetHeroName { hero { name } }");

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL("StarWars")
                .ModifyRequestOptions(o => o.PersistedOperations.OnlyAllowPersistedDocuments = true)
                .ConfigureSchemaServices(c => c.AddSingleton<IOperationDocumentStorage>(storage))
                .UsePersistedOperationPipeline());

        const string query = "query GetHeroName { hero { name } }";

        // act
        var result = await server.PostAsync(
            new ClientQueryRequest { Query = query },
            path: "/starwars");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Standard_Query_Allowed_When_Persisted()
    {
        // arrange
        var storage = new OperationStorage();
        storage.AddOperation(
            "a73defcdf38e5891e91b9ba532cf4c36",
            "query GetHeroName { hero { name } }");

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL("StarWars")
                .ModifyRequestOptions(o =>
                {
                    o.PersistedOperations.OnlyAllowPersistedDocuments = true;
                    o.PersistedOperations.AllowDocumentBody = true;
                })
                .ConfigureSchemaServices(c => c.AddSingleton<IOperationDocumentStorage>(storage))
                .UsePersistedOperationPipeline());

        const string query = "query GetHeroName { hero { name } }";

        // act
        var result = await server.PostAsync(
            new ClientQueryRequest { Query = query },
            path: "/starwars");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Standard_Query_Not_Allowed_Custom_Error()
    {
        // arrange
        var storage = new OperationStorage();

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL("StarWars")
                .ModifyRequestOptions(o =>
                {
                    o.PersistedOperations.OnlyAllowPersistedDocuments = true;
                    o.PersistedOperations.OperationNotAllowedError =
                        ErrorBuilder.New()
                            .SetMessage("Not allowed!")
                            .Build();
                })
                .ConfigureSchemaServices(c => c.AddSingleton<IOperationDocumentStorage>(storage))
                .UsePersistedOperationPipeline());

        const string query = "{ __typename }";

        // act
        var result = await server.PostAsync(
            new ClientQueryRequest { Query = query },
            path: "/starwars");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Standard_Query_Not_Allowed_Override_Per_Request()
    {
        // arrange
        var storage = new OperationStorage();

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL("StarWars")
                .ModifyRequestOptions(o =>
                {
                    o.PersistedOperations.OnlyAllowPersistedDocuments = true;
                    o.PersistedOperations.AllowDocumentBody = true;
                })
                .ConfigureSchemaServices(c => c.AddSingleton<IOperationDocumentStorage>(storage))
                .UsePersistedOperationPipeline()
                .AddHttpRequestInterceptor<AllowNonPersistedOperationInterceptor>());

        const string query = "{ __typename }";

        // act
        var result = await server.PostAsync(
            new ClientQueryRequest { Query = query },
            path: "/starwars");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Ensure_Pooled_Objects_Are_Cleared()
    {
        // arrange
        // we have one operation in our storage that is allowed.
        var storage = new OperationStorage();
        storage.AddOperation(
            "a73defcdf38e5891e91b9ba532cf4c36",
            "query GetHeroName { hero { name } }");

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL("StarWars")
                .ModifyRequestOptions(o =>
                {
                    // we only allow persisted operations, but we also allow standard requests
                    // as long as they match a persisted operation.
                    o.PersistedOperations.OnlyAllowPersistedDocuments = true;
                    o.PersistedOperations.AllowDocumentBody = true;
                })
                .ConfigureSchemaServices(c => c.AddSingleton<IOperationDocumentStorage>(storage))
                .UsePersistedOperationPipeline());

        // act
        var result1ShouldBeOk = await server.PostAsync(
            new ClientQueryRequest { Id = "a73defcdf38e5891e91b9ba532cf4c36" },
            path: "/starwars");

        var result2ShouldBeOk = await server.PostAsync(
            new ClientQueryRequest { Query = "query GetHeroName { hero { name } }" },
            path: "/starwars");

        var result3ShouldFail = await server.PostAsync(
            new ClientQueryRequest { Query = "{ __typename }" },
            path: "/starwars");

        // assert
        await Snapshot.Create()
            .Add(result1ShouldBeOk, "Result 1 - Should be OK")
            .Add(result2ShouldBeOk, "Result 2 - Should be OK")
            .Add(result3ShouldFail, "Result 3 - Should fail")
            .MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Post_Should_NotPersistRequestDocument_When_HashDoesNotMatchQuery()
    {
        // arrange
        var storage = new OperationStorage();

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL("StarWars")
                .ConfigureSchemaServices(c => c.AddSingleton<IOperationDocumentStorage>(storage))
                .UseAutomaticPersistedOperationPipeline());

        var request = CreateApolloStyleRequest("md5Hash", "acb8d5d513c260b3cef3e3a12b0e29af");
        request.Id = "acb8d5d513c260b3cef3e3a12b0e29af";
        request.Query = "{ __typename }";

        // act
        var result = await server.PostAsync(request, path: "/starwars");

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "ContentType": "application/graphql-response+json; charset=utf-8",
              "StatusCode": "OK",
              "Data": {
                "__typename": "Query"
              },
              "Errors": null,
              "Extensions": {
                "persistedQuery": {
                  "md5Hash": "acb8d5d513c260b3cef3e3a12b0e29af",
                  "expectedHashValue": "ef5c9e7b1e24de261642083d4e294322",
                  "expectedHashType": "md5Hash",
                  "expectedHashFormat": "Hex",
                  "persisted": false
                }
              }
            }
            """);
        Assert.Empty(storage.Keys);
    }

    [Fact]
    public async Task Post_Should_UseStoredDocument_When_HashIsAlreadyStored()
    {
        // arrange
        var storage = new OperationStorage();
        storage.AddOperation("acb8d5d513c260b3cef3e3a12b0e29af", "{ hero { name } }");

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL("StarWars")
                .ConfigureSchemaServices(c => c.AddSingleton<IOperationDocumentStorage>(storage))
                .UseAutomaticPersistedOperationPipeline());

        var request = CreateApolloStyleRequest("md5Hash", "acb8d5d513c260b3cef3e3a12b0e29af");
        request.Id = "acb8d5d513c260b3cef3e3a12b0e29af";
        request.Query = "{ __typename }";

        // act
        var result = await server.PostAsync(request, path: "/starwars");

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "ContentType": "application/graphql-response+json; charset=utf-8",
              "StatusCode": "OK",
              "Data": {
                "hero": {
                  "name": "R2-D2"
                }
              },
              "Errors": null,
              "Extensions": null
            }
            """);
        Assert.Equal(
            "{ hero { name } }",
            storage.PrintOperation("acb8d5d513c260b3cef3e3a12b0e29af"));
    }

    private ClientQueryRequest CreateApolloStyleRequest(string hashName, string key)
        => new()
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

    private sealed class OperationStorage : IOperationDocumentStorage
    {
        private readonly Dictionary<string, OperationDocument> _cache =
            new(StringComparer.Ordinal);

        public IReadOnlyCollection<string> Keys => _cache.Keys;

        public ValueTask<IOperationDocument?> TryReadAsync(
            OperationDocumentId documentId,
            CancellationToken cancellationToken = default)
            => _cache.TryGetValue(documentId.Value, out var value)
                ? new ValueTask<IOperationDocument?>(value)
                : new ValueTask<IOperationDocument?>(default(IOperationDocument));

        public ValueTask SaveAsync(
            OperationDocumentId documentId,
            IOperationDocument document,
            CancellationToken cancellationToken = default)
        {
            var syntaxTree = Utf8GraphQLParser.Parse(document.AsSpan());
            _cache[documentId.Value] = new OperationDocument(syntaxTree);

            return default;
        }

        public void AddOperation(string key, string sourceText)
        {
            var doc = new OperationDocument(Utf8GraphQLParser.Parse(sourceText));
            _cache.Add(key, doc);
        }

        public string PrintOperation(string key)
            => _cache[key].Document.Print(indented: false);
    }

    private sealed class AllowNonPersistedOperationInterceptor : DefaultHttpRequestInterceptor
    {
        public override ValueTask OnCreateAsync(
            HttpContext context,
            IRequestExecutor requestExecutor,
            OperationRequestBuilder requestBuilder,
            CancellationToken cancellationToken)
        {
            requestBuilder.AllowNonPersistedOperation();
            return base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
        }
    }
}
