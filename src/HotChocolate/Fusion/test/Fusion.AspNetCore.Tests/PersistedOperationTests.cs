using System.Net;
using System.Text;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Language;
using HotChocolate.PersistedOperations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using GraphQLHttpClient = HotChocolate.Transport.Http.GraphQLHttpClient;
using GraphQLHttpMethod = HotChocolate.Transport.Http.GraphQLHttpMethod;
using GraphQLHttpRequest = HotChocolate.Transport.Http.GraphQLHttpRequest;
using OperationRequest = HotChocolate.Transport.OperationRequest;

namespace HotChocolate.Fusion;

public class PersistedOperationTests : FusionTestBase
{
    [Fact]
    public async Task HotChocolateStyle_MD5Hash_Success()
    {
        // arrange
        var storage = new OperationStorage();
        var hashProvider = new MD5DocumentHashProvider(HashFormat.Hex);

        using var gateway = await CreateGatewayAsync(b => b
            .ConfigureSchemaServices((_, sc) => sc.AddSingleton<IOperationDocumentStorage>(storage))
            .UsePersistedOperationPipeline());

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        const string query = "{ __typename }";
        var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
        storage.AddOperation(key.Value, query);

        var request = new OperationRequest(id: key.Value);

        // act
        using var result = await client.SendAsync(
            new GraphQLHttpRequest(request, new Uri("http://localhost:5000/graphql"))
            {
                Method = GraphQLHttpMethod.Post
            },
            TestContext.Current.CancellationToken);

        // arrange
        result.HttpResponseMessage.MatchSnapshot();
    }

    [Fact]
    public async Task HotChocolateStyle_MD5Hash_NotFound()
    {
        // arrange
        var storage = new OperationStorage();
        var hashProvider = new MD5DocumentHashProvider(HashFormat.Hex);

        using var gateway = await CreateGatewayAsync(b => b
            .ConfigureSchemaServices((_, sc) => sc.AddSingleton<IOperationDocumentStorage>(storage))
            .UsePersistedOperationPipeline());

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        const string query = "{ __typename }";
        var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
        // we are not adding the query to the store so the server request should fail
        // storage.AddOperation(key, query);

        var request = new OperationRequest(id: key.Value);

        // act
        using var result = await client.SendAsync(
            new GraphQLHttpRequest(request, new Uri("http://localhost:5000/graphql"))
            {
                Method = GraphQLHttpMethod.Post
            },
            TestContext.Current.CancellationToken);

        // assert
        result.HttpResponseMessage.MatchSnapshot();
    }

    [Fact]
    public async Task HotChocolateStyle_Sha1Hash_Success()
    {
        // arrange
        var storage = new OperationStorage();
        var hashProvider = new Sha1DocumentHashProvider(HashFormat.Hex);

        using var gateway = await CreateGatewayAsync(b => b
            .AddSha1DocumentHashProvider(HashFormat.Hex)
            .ConfigureSchemaServices((_, sc) => sc.AddSingleton<IOperationDocumentStorage>(storage))
            .UsePersistedOperationPipeline());

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        const string query = "{ __typename }";
        var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
        storage.AddOperation(key.Value, query);

        var request = new OperationRequest(id: key.Value);

        // act
        using var result = await client.SendAsync(
            new GraphQLHttpRequest(request, new Uri("http://localhost:5000/graphql"))
            {
                Method = GraphQLHttpMethod.Post
            },
            TestContext.Current.CancellationToken);

        // assert
        result.HttpResponseMessage.MatchSnapshot();
    }

    [Fact]
    public async Task HotChocolateStyle_Sha256Hash_Success()
    {
        // arrange
        var storage = new OperationStorage();
        var hashProvider = new Sha256DocumentHashProvider(HashFormat.Hex);

        using var gateway = await CreateGatewayAsync(b => b
            .AddSha256DocumentHashProvider(HashFormat.Hex)
            .ConfigureSchemaServices((_, sc) => sc.AddSingleton<IOperationDocumentStorage>(storage))
            .UsePersistedOperationPipeline());

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        const string query = "{ __typename }";
        var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
        storage.AddOperation(key.Value, query);

        var request = new OperationRequest(id: key.Value);

        // act
        using var result = await client.SendAsync(
            new GraphQLHttpRequest(request, new Uri("http://localhost:5000/graphql"))
            {
                Method = GraphQLHttpMethod.Post
            },
            TestContext.Current.CancellationToken);

        // assert
        result.HttpResponseMessage.MatchSnapshot();
    }

    [Fact]
    public async Task HotChocolateStyle_Sha256Hash_Query_Empty_String_Success()
    {
        // arrange
        var storage = new OperationStorage();
        var hashProvider = new Sha256DocumentHashProvider(HashFormat.Hex);

        using var gateway = await CreateGatewayAsync(b => b
            .AddSha256DocumentHashProvider(HashFormat.Hex)
            .ConfigureSchemaServices((_, sc) => sc.AddSingleton<IOperationDocumentStorage>(storage))
            .UsePersistedOperationPipeline());

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        const string query = "{ __typename }";
        var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
        storage.AddOperation(key.Value, query);

        var request = new OperationRequest(query: string.Empty, id: key.Value);

        // act
        using var result = await client.SendAsync(
            new GraphQLHttpRequest(request, new Uri("http://localhost:5000/graphql"))
            {
                Method = GraphQLHttpMethod.Post
            },
            TestContext.Current.CancellationToken);

        // assert
        result.HttpResponseMessage.MatchSnapshot();
    }

    [Fact]
    public async Task ApolloStyle_MD5Hash_Success()
    {
        // arrange
        var storage = new OperationStorage();
        var hashProvider = new MD5DocumentHashProvider(HashFormat.Hex);

        using var gateway = await CreateGatewayAsync(b => b
            .ConfigureSchemaServices((_, sc) => sc.AddSingleton<IOperationDocumentStorage>(storage))
            .UsePersistedOperationPipeline());

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        const string query = "{ __typename }";
        var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
        storage.AddOperation(key.Value, query);

        var request = CreateApolloStyleRequest(hashProvider.Name, key.Value);

        // act
        using var result = await client.SendAsync(
            new GraphQLHttpRequest(request, new Uri("http://localhost:5000/graphql"))
            {
                Method = GraphQLHttpMethod.Post
            },
            TestContext.Current.CancellationToken);

        // assert
        result.HttpResponseMessage.MatchSnapshot();
    }

    [Fact]
    public async Task ApolloStyle_MD5Hash_NotFound()
    {
        // arrange
        var storage = new OperationStorage();
        var hashProvider = new MD5DocumentHashProvider(HashFormat.Hex);

        using var gateway = await CreateGatewayAsync(b => b
            .ConfigureSchemaServices((_, sc) => sc.AddSingleton<IOperationDocumentStorage>(storage))
            .UsePersistedOperationPipeline());

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        const string query = "{ __typename }";
        var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
        // we are not adding the query to the store so the server request should fail
        // storage.AddOperation(key, query);

        var request = CreateApolloStyleRequest(hashProvider.Name, key.Value);

        // act
        using var result = await client.SendAsync(
            new GraphQLHttpRequest(request, new Uri("http://localhost:5000/graphql"))
            {
                Method = GraphQLHttpMethod.Post
            },
            TestContext.Current.CancellationToken);

        // assert
        result.HttpResponseMessage.MatchSnapshot();
    }

    [Fact]
    public async Task ApolloStyle_Sha1Hash_Success()
    {
        // arrange
        var storage = new OperationStorage();
        var hashProvider = new Sha1DocumentHashProvider(HashFormat.Hex);

        using var gateway = await CreateGatewayAsync(b => b
            .AddSha1DocumentHashProvider(HashFormat.Hex)
            .ConfigureSchemaServices((_, sc) => sc.AddSingleton<IOperationDocumentStorage>(storage))
            .UsePersistedOperationPipeline());

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        const string query = "{ __typename }";
        var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
        storage.AddOperation(key.Value, query);

        var request = CreateApolloStyleRequest(hashProvider.Name, key.Value);

        // act
        using var result = await client.SendAsync(
            new GraphQLHttpRequest(request, new Uri("http://localhost:5000/graphql"))
            {
                Method = GraphQLHttpMethod.Post
            },
            TestContext.Current.CancellationToken);

        // assert
        result.HttpResponseMessage.MatchSnapshot();
    }

    [Fact]
    public async Task ApolloStyle_Sha256Hash_Success()
    {
        // arrange
        var storage = new OperationStorage();
        var hashProvider = new Sha256DocumentHashProvider(HashFormat.Hex);

        using var gateway = await CreateGatewayAsync(b => b
            .AddSha256DocumentHashProvider(HashFormat.Hex)
            .ConfigureSchemaServices((_, sc) => sc.AddSingleton<IOperationDocumentStorage>(storage))
            .UsePersistedOperationPipeline());

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        const string query = "{ __typename }";
        var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
        storage.AddOperation(key.Value, query);

        var request = CreateApolloStyleRequest(hashProvider.Name, key.Value);

        // act
        using var result = await client.SendAsync(
            new GraphQLHttpRequest(request, new Uri("http://localhost:5000/graphql"))
            {
                Method = GraphQLHttpMethod.Post
            },
            TestContext.Current.CancellationToken);

        // assert
        result.HttpResponseMessage.MatchSnapshot();
    }

    [Fact]
    public async Task Standard_Query_By_Default_Works()
    {
        // arrange
        var storage = new OperationStorage();

        using var gateway = await CreateGatewayAsync(b => b
            .ConfigureSchemaServices((_, sc) => sc.AddSingleton<IOperationDocumentStorage>(storage))
            .UsePersistedOperationPipeline());

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        const string query = "{ __typename }";

        var request = new OperationRequest(query: query);

        // act
        using var result = await client.SendAsync(
            new GraphQLHttpRequest(request, new Uri("http://localhost:5000/graphql"))
            {
                Method = GraphQLHttpMethod.Post
            },
            TestContext.Current.CancellationToken);

        // assert
        result.HttpResponseMessage.MatchSnapshot();
    }

    [Fact]
    public async Task Standard_Query_Not_Allowed()
    {
        // arrange
        var storage = new OperationStorage();

        using var gateway = await CreateGatewayAsync(b => b
            .ModifyRequestOptions(o => o.PersistedOperations.OnlyAllowPersistedDocuments = true)
            .ConfigureSchemaServices((_, sc) => sc.AddSingleton<IOperationDocumentStorage>(storage))
            .UsePersistedOperationPipeline());

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        const string query = "{ __typename }";

        var request = new OperationRequest(query: query);

        // act
        using var result = await client.SendAsync(
            new GraphQLHttpRequest(request, new Uri("http://localhost:5000/graphql"))
            {
                Method = GraphQLHttpMethod.Post
            },
            TestContext.Current.CancellationToken);

        // assert
        result.HttpResponseMessage.MatchSnapshot();
    }

    [Fact]
    public async Task Standard_Query_Not_Allowed_Even_When_Persisted()
    {
        // arrange
        var storage = new OperationStorage();
        var hashProvider = new MD5DocumentHashProvider(HashFormat.Hex);

        using var gateway = await CreateGatewayAsync(b => b
            .ModifyRequestOptions(o => o.PersistedOperations.OnlyAllowPersistedDocuments = true)
            .ConfigureSchemaServices((_, sc) => sc.AddSingleton<IOperationDocumentStorage>(storage))
            .UsePersistedOperationPipeline());

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        const string query = "{ __typename }";
        var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
        storage.AddOperation(key.Value, query);

        var request = new OperationRequest(query: query);

        // act
        using var result = await client.SendAsync(
            new GraphQLHttpRequest(request, new Uri("http://localhost:5000/graphql"))
            {
                Method = GraphQLHttpMethod.Post
            },
            TestContext.Current.CancellationToken);

        // assert
        result.HttpResponseMessage.MatchSnapshot();
    }

    [Fact]
    public async Task Standard_Query_Allowed_When_Persisted()
    {
        // arrange
        var storage = new OperationStorage();
        var hashProvider = new MD5DocumentHashProvider(HashFormat.Hex);

        using var gateway = await CreateGatewayAsync(b => b
            .ModifyRequestOptions(o =>
            {
                o.PersistedOperations.OnlyAllowPersistedDocuments = true;
                o.PersistedOperations.AllowDocumentBody = true;
            })
            .ConfigureSchemaServices((_, sc) => sc.AddSingleton<IOperationDocumentStorage>(storage))
            .UsePersistedOperationPipeline());

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        const string query = "{ __typename }";
        var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
        storage.AddOperation(key.Value, query);

        var request = new OperationRequest(query: query);

        // act
        using var result = await client.SendAsync(
            new GraphQLHttpRequest(request, new Uri("http://localhost:5000/graphql"))
            {
                Method = GraphQLHttpMethod.Post
            },
            TestContext.Current.CancellationToken);

        // assert
        result.HttpResponseMessage.MatchSnapshot();
    }

    [Fact]
    public async Task Standard_Query_Not_Allowed_Custom_Error()
    {
        // arrange
        var storage = new OperationStorage();

        using var gateway = await CreateGatewayAsync(b => b
            .ModifyRequestOptions(o =>
            {
                o.PersistedOperations.OnlyAllowPersistedDocuments = true;
                o.PersistedOperations.OperationNotAllowedError =
                    ErrorBuilder.New()
                        .SetMessage("Not allowed!")
                        .Build();
            })
            .ConfigureSchemaServices((_, sc) => sc.AddSingleton<IOperationDocumentStorage>(storage))
            .UsePersistedOperationPipeline());

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        const string query = "{ __typename }";

        var request = new OperationRequest(query: query);

        // act
        using var result = await client.SendAsync(
            new GraphQLHttpRequest(request, new Uri("http://localhost:5000/graphql"))
            {
                Method = GraphQLHttpMethod.Post
            },
            TestContext.Current.CancellationToken);

        // assert
        result.HttpResponseMessage.MatchSnapshot();
    }

    [Fact]
    public async Task Standard_Query_Not_Allowed_Override_Per_Request()
    {
        // arrange
        var storage = new OperationStorage();

        using var gateway = await CreateGatewayAsync(b => b
            .ModifyRequestOptions(o =>
            {
                o.PersistedOperations.OnlyAllowPersistedDocuments = true;
                o.PersistedOperations.AllowDocumentBody = true;
            })
            .ConfigureSchemaServices((_, sc) => sc.AddSingleton<IOperationDocumentStorage>(storage))
            .UsePersistedOperationPipeline()
            .AddHttpRequestInterceptor<AllowNonPersistedOperationInterceptor>());

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        const string query = "{ __typename }";

        var request = new OperationRequest(query: query);

        // act
        using var result = await client.SendAsync(
            new GraphQLHttpRequest(request, new Uri("http://localhost:5000/graphql"))
            {
                Method = GraphQLHttpMethod.Post
            },
            TestContext.Current.CancellationToken);

        // assert
        result.HttpResponseMessage.MatchSnapshot();
    }

    [Fact]
    public async Task Post_Should_RefuseVariableBatch_When_BatchingIsDisabled()
    {
        // arrange
        var storage = new OperationStorage();
        storage.AddOperation(
            "getField",
            "query GetField($skip: Boolean!) { field @skip(if: $skip) }");

        var server = CreateSourceSchema(
            "A",
            """
            type Query {
                field: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureApplication: app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapGraphQLPersistedOperations());
            },
            configureGatewayBuilder: b => b
                .ConfigureSchemaServices(
                    (_, s) => s.AddSingleton<IOperationDocumentStorage>(storage))
                .UsePersistedOperationPipeline()
                .ModifyServerOptions(o => o.Batching = AllowedBatching.None),
            includeOperationPlan: false);

        using var client = gateway.CreateClient();

        // act
        using var response = await client.PostAsync(
            "http://localhost:5000/graphql/persisted/getField",
            new StringContent(
                """{ "variables": [{ "skip": true }, { "skip": false }] }""",
                Encoding.UTF8,
                "application/json"),
            TestContext.Current.CancellationToken);

        // assert
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                """
                Headers:
                Vary: Accept
                Content-Type: application/graphql-response+json; charset=utf-8
                Content-Length: 82
                -------------------------->
                Status Code: BadRequest
                -------------------------->
                {"errors":[{"message":"Invalid GraphQL Request.","extensions":{"code":"HC0009"}}]}
                """);
    }

    [Fact]
    public async Task Post_Should_ExecuteSentQuery_When_PreviousRequestSentDocumentId()
    {
        // arrange
        var server = CreateSourceSchema(
            "A",
            """
            type Query {
                field: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            includeOperationPlan: false);

        using var client = gateway.CreateClient();

        using var firstResponse = await client.PostAsync(
            "http://localhost:5000/graphql",
            new StringContent(
                """{ "id": "388c5f9c43fb589b1c5d1e144121613b", "query": "{ __typename }" }""",
                Encoding.UTF8,
                "application/json"),
            TestContext.Current.CancellationToken);

        // act
        using var response = await client.PostAsync(
            "http://localhost:5000/graphql",
            new StringContent(
                """{ "query": "{ field }" }""",
                Encoding.UTF8,
                "application/json"),
            TestContext.Current.CancellationToken);

        // assert
        Snapshot
            .Create()
            .Add(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .MatchInline(
                """
                {"data":{"field":"Query"}}
                """);
    }

    [Theory]
    [InlineData(HttpTransportVersion.Legacy, "text/plain", HttpStatusCode.NotFound)]
    [InlineData(HttpTransportVersion.Legacy, null, HttpStatusCode.NotFound)]
    [InlineData(HttpTransportVersion.Draft20250508, "text/plain", HttpStatusCode.NotFound)]
    [InlineData(HttpTransportVersion.Draft20250508, null, HttpStatusCode.NotFound)]
    [InlineData(
        HttpTransportVersion.Draft20260903,
        "text/plain",
        HttpStatusCode.UnsupportedMediaType)]
    [InlineData(HttpTransportVersion.Draft20260903, null, HttpStatusCode.UnsupportedMediaType)]
    public async Task Post_Should_ReturnUnsupportedMediaType_When_ContentTypeIsNotJson(
        HttpTransportVersion transportVersion,
        string? contentType,
        HttpStatusCode expectedStatusCode)
    {
        // arrange
        var storage = new OperationStorage();
        storage.AddOperation("getField", "query GetField { field }");

        var server = CreateSourceSchema(
            "A",
            """
            type Query {
                field: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureApplication: app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapGraphQLPersistedOperations());
            },
            configureGatewayBuilder: b => b
                .ConfigureSchemaServices(
                    (_, s) => s.AddSingleton<IOperationDocumentStorage>(storage))
                .UsePersistedOperationPipeline()
                .AddHttpResponseFormatter(
                    new HttpResponseFormatterOptions
                    {
                        HttpTransportVersion = transportVersion
                    }),
            includeOperationPlan: false);

        using var client = gateway.CreateClient();

        // act
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "http://localhost:5000/graphql/persisted/getField/GetField");
        request.Content = contentType is null
            ? new ByteArrayContent(Encoding.UTF8.GetBytes("{ }"))
            : new StringContent("{ }", Encoding.UTF8, contentType);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(expectedStatusCode, response.StatusCode);
        Assert.Equal(
            "",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(HttpTransportVersion.Draft20250508, HttpStatusCode.NotFound, new string[0])]
    [InlineData(
        HttpTransportVersion.Draft20260903,
        HttpStatusCode.MethodNotAllowed,
        new[] { "GET", "OPTIONS", "POST" })]
    public async Task Put_Should_ReturnMethodNotAllowed_When_PersistedRoutesAreMappedAlone(
        HttpTransportVersion transportVersion,
        HttpStatusCode expectedStatusCode,
        string[] expectedAllow)
    {
        // arrange
        var storage = new OperationStorage();
        storage.AddOperation("getField", "query GetField { field }");

        var server = CreateSourceSchema(
            "A",
            """
            type Query {
                field: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureApplication: app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapGraphQLPersistedOperations());
            },
            configureGatewayBuilder: b => b
                .ConfigureSchemaServices(
                    (_, s) => s.AddSingleton<IOperationDocumentStorage>(storage))
                .UsePersistedOperationPipeline()
                .AddHttpResponseFormatter(
                    new HttpResponseFormatterOptions
                    {
                        HttpTransportVersion = transportVersion
                    }),
            includeOperationPlan: false);

        using var client = gateway.CreateClient();

        // act
        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            "http://localhost:5000/graphql/persisted/getField/GetField");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(expectedStatusCode, response.StatusCode);
        Assert.Equal(expectedAllow, response.Content.Headers.Allow);
    }

    [Theory]
    [InlineData(HttpTransportVersion.Draft20250508, HttpStatusCode.NotFound, new string[0])]
    [InlineData(
        HttpTransportVersion.Draft20260903,
        HttpStatusCode.NoContent,
        new[] { "GET", "OPTIONS", "POST" })]
    public async Task Options_Should_ReturnAllowedMethods_When_PersistedRoutesAreMappedAlone(
        HttpTransportVersion transportVersion,
        HttpStatusCode expectedStatusCode,
        string[] expectedAllow)
    {
        // arrange
        var storage = new OperationStorage();
        storage.AddOperation("getField", "query GetField { field }");

        var server = CreateSourceSchema(
            "A",
            """
            type Query {
                field: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureApplication: app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapGraphQLPersistedOperations());
            },
            configureGatewayBuilder: b => b
                .ConfigureSchemaServices(
                    (_, s) => s.AddSingleton<IOperationDocumentStorage>(storage))
                .UsePersistedOperationPipeline()
                .AddHttpResponseFormatter(
                    new HttpResponseFormatterOptions
                    {
                        HttpTransportVersion = transportVersion
                    }),
            includeOperationPlan: false);

        using var client = gateway.CreateClient();

        // act
        using var request = new HttpRequestMessage(
            HttpMethod.Options,
            "http://localhost:5000/graphql/persisted/getField/GetField");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(expectedStatusCode, response.StatusCode);
        Assert.Equal(expectedAllow, response.Content.Headers.Allow);
    }

    private static OperationRequest CreateApolloStyleRequest(
        string hashName,
        string id)
    {
        return new OperationRequest(
            extensions: new Dictionary<string, object?>
            {
                ["persistedQuery"] = new Dictionary<string, object?>
                {
                    ["version"] = 1,
                    [hashName] = id
                }
            });
    }

    private async Task<Gateway> CreateGatewayAsync(Action<IFusionGatewayBuilder> configureBuilder)
    {
        var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              field: String!
            }
            """);

        return await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ], configureGatewayBuilder: b =>
        {
            b.AddHttpRequestInterceptor<DefaultHttpRequestInterceptor>();
            configureBuilder(b);
        });
    }

    private sealed class OperationStorage : IOperationDocumentStorage
    {
        private readonly Dictionary<string, OperationDocument> _cache =
            new(StringComparer.Ordinal);

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
            => throw new NotImplementedException();

        public void AddOperation(string key, string sourceText)
        {
            var doc = new OperationDocument(Utf8GraphQLParser.Parse(sourceText));
            _cache.Add(key, doc);
        }
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
