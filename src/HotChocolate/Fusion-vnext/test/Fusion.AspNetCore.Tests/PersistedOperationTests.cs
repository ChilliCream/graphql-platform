using System.Text;
using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Transport.Http;
using HotChocolate.Language;
using HotChocolate.PersistedOperations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

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
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        result.HttpResponseMessage.MatchSnapshot();
    }

    // [Fact]
    // public async Task HotChocolateStyle_Sha1Hash_Success()
    // {
    //     // arrange
    //     var storage = new OperationStorage();
    //     var hashProvider = new Sha1DocumentHashProvider(HashFormat.Hex);
    //
    //     using var gateway = await CreateGatewayAsync(b => b
    //         .AddSha1DocumentHashProvider(HashFormat.Hex)
    //         .ConfigureSchemaServices((_, sc) => sc.AddSingleton<IOperationDocumentStorage>(storage))
    //         .UsePersistedOperationPipeline());
    //
    //     using var client = GraphQLHttpClient.Create(gateway.CreateClient());
    //
    //     const string query = "{ __typename }";
    //     var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
    //     storage.AddOperation(key.Value, query);
    //
    //     var request = new OperationRequest(id: key.Value);
    //
    //     // act
    //     using var result = await client.PostAsync(
    //         request,
    //         new Uri("http://localhost:5000/graphql"));
    //
    //     // assert
    //     result.HttpResponseMessage.MatchSnapshot();
    // }

    // [Fact]
    // public async Task HotChocolateStyle_Sha256Hash_Success()
    // {
    //     // arrange
    //     var storage = new OperationStorage();
    //     var hashProvider = new Sha256DocumentHashProvider(HashFormat.Hex);
    //
    //     using var gateway = await CreateGatewayAsync(b => b
    //         .AddSha256DocumentHashProvider(HashFormat.Hex)
    //         .ConfigureSchemaServices((_, sc) => sc.AddSingleton<IOperationDocumentStorage>(storage))
    //         .UsePersistedOperationPipeline());
    //
    //     using var client = GraphQLHttpClient.Create(gateway.CreateClient());
    //
    //     const string query = "{ __typename }";
    //     var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
    //     storage.AddOperation(key.Value, query);
    //
    //     var request = new OperationRequest(id: key.Value);
    //
    //     // act
    //     using var result = await client.PostAsync(
    //         request,
    //         new Uri("http://localhost:5000/graphql"));
    //
    //     // assert
    //     result.HttpResponseMessage.MatchSnapshot();
    // }

    // [Fact]
    // public async Task HotChocolateStyle_Sha256Hash_Query_Empty_String_Success()
    // {
    //     // arrange
    //     var storage = new OperationStorage();
    //     var hashProvider = new Sha256DocumentHashProvider(HashFormat.Hex);
    //
    //     using var gateway = await CreateGatewayAsync(b => b
    //         .AddSha256DocumentHashProvider(HashFormat.Hex)
    //         .ConfigureSchemaServices((_, sc) => sc.AddSingleton<IOperationDocumentStorage>(storage))
    //         .UsePersistedOperationPipeline());
    //
    //     using var client = GraphQLHttpClient.Create(gateway.CreateClient());
    //
    //     const string query = "{ __typename }";
    //     var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
    //     storage.AddOperation(key.Value, query);
    //
    //     var request = new OperationRequest(query: string.Empty, id: key.Value);
    //
    //     // act
    //     using var result = await client.PostAsync(
    //         request,
    //         new Uri("http://localhost:5000/graphql"));
    //
    //     // assert
    //     result.HttpResponseMessage.MatchSnapshot();
    // }

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
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

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
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        result.HttpResponseMessage.MatchSnapshot();
    }

    // [Fact]
    // public async Task ApolloStyle_Sha1Hash_Success()
    // {
    //     // arrange
    //     var storage = new OperationStorage();
    //     var hashProvider = new Sha1DocumentHashProvider(HashFormat.Hex);
    //
    //     using var gateway = await CreateGatewayAsync(b => b
    //         .AddSha1DocumentHashProvider(HashFormat.Hex)
    //         .ConfigureSchemaServices((_, sc) => sc.AddSingleton<IOperationDocumentStorage>(storage))
    //         .UsePersistedOperationPipeline());
    //
    //     using var client = GraphQLHttpClient.Create(gateway.CreateClient());
    //
    //     const string query = "{ __typename }";
    //     var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
    //     storage.AddOperation(key.Value, query);
    //
    //     var request = CreateApolloStyleRequest(hashProvider.Name, key.Value);
    //
    //     // act
    //     using var result = await client.PostAsync(
    //         request,
    //         new Uri("http://localhost:5000/graphql"));
    //
    //     // assert
    //     result.HttpResponseMessage.MatchSnapshot();
    // }

    // [Fact]
    // public async Task ApolloStyle_Sha256Hash_Success()
    // {
    //     // arrange
    //     var storage = new OperationStorage();
    //     var hashProvider = new Sha256DocumentHashProvider(HashFormat.Hex);
    //
    //     using var gateway = await CreateGatewayAsync(b => b
    //         .AddSha256DocumentHashProvider(HashFormat.Hex)
    //         .ConfigureSchemaServices((_, sc) => sc.AddSingleton<IOperationDocumentStorage>(storage))
    //         .UsePersistedOperationPipeline());
    //
    //     using var client = GraphQLHttpClient.Create(gateway.CreateClient());
    //
    //     const string query = "{ __typename }";
    //     var key = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));
    //     storage.AddOperation(key.Value, query);
    //
    //     var request = CreateApolloStyleRequest(hashProvider.Name, key.Value);
    //
    //     // act
    //     using var result = await client.PostAsync(
    //         request,
    //         new Uri("http://localhost:5000/graphql"));
    //
    //     // assert
    //     result.HttpResponseMessage.MatchSnapshot();
    // }

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
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

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
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        result.HttpResponseMessage.MatchSnapshot();
    }

    [Fact]
    public async Task Standard_Query_Not_Allowed_Even_When_Persisted()
    {
        // arrange
        var storage = new OperationStorage();
        storage.AddOperation(
            "a73defcdf38e5891e91b9ba532cf4c36",
            "query GetHeroName { hero { name } }");

        using var gateway = await CreateGatewayAsync(b => b
            .ModifyRequestOptions(o => o.PersistedOperations.OnlyAllowPersistedDocuments = true)
            .ConfigureSchemaServices((_, sc) => sc.AddSingleton<IOperationDocumentStorage>(storage))
            .UsePersistedOperationPipeline());

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        const string query = "query GetHeroName { hero { name } }";

        var request = new OperationRequest(query: query);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        result.HttpResponseMessage.MatchSnapshot();
    }

    [Fact]
    public async Task Standard_Query_Allowed_When_Persisted()
    {
        // arrange
        var storage = new OperationStorage();
        storage.AddOperation(
            "a73defcdf38e5891e91b9ba532cf4c36",
            "query GetHeroName { hero { name } }");

        using var gateway = await CreateGatewayAsync(b => b
            .ModifyRequestOptions(o =>
            {
                o.PersistedOperations.OnlyAllowPersistedDocuments = true;
                o.PersistedOperations.AllowDocumentBody = true;
            })
            .ConfigureSchemaServices((_, sc) => sc.AddSingleton<IOperationDocumentStorage>(storage))
            .UsePersistedOperationPipeline());

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        const string query = "query GetHeroName { hero { name } }";

        var request = new OperationRequest(query: query);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

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
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        result.HttpResponseMessage.MatchSnapshot();
    }

    [Fact]
    public async Task Standard_Query_Not_Allowed_Override_Per_Request()
    {
        // arrange
        var storage = new OperationStorage();

        using var gateway = await CreateGatewayAsync(b => b
            .ModifyRequestOptions(o => o.PersistedOperations.OnlyAllowPersistedDocuments = true)
            .ConfigureSchemaServices((_, sc) => sc.AddSingleton<IOperationDocumentStorage>(storage))
            .UsePersistedOperationPipeline()
            .AddHttpRequestInterceptor<AllowNonPersistedOperationInterceptor>());

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        const string query = "{ __typename }";

        var request = new OperationRequest(query: query);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        result.HttpResponseMessage.MatchSnapshot();
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
