using System.Buffers;
using System.Collections.Immutable;
using System.Text;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Clients.AliasBatching;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public class AliasBatchingHttpSourceSchemaClientFactoryTests : FusionTestBase
{
    [Fact]
    public async Task GetOrCreateOperation_Should_ReturnCachedInstance_When_CalledTwiceWithSameRequests()
    {
        // arrange
        var schema = CreateCompositeSchema();
        var httpClientFactory = CreateHttpClientFactory();
        var aliasFactory = new AliasBatchingHttpSourceSchemaClientFactory(httpClientFactory);
        var configuration = new HttpSourceSchemaClientConfiguration(
            "a",
            new Uri("http://localhost:5000/graphql"),
            aliasBatching: true);
        await using var client =
            (AliasBatchingHttpSourceSchemaClient)aliasFactory.CreateClient(schema, configuration);

        var requests = ImmutableArray.Create(
            Request(
                """
                query Op($__fusion_2_id: ID!) {
                  productById(id: $__fusion_2_id) { name }
                }
                """,
                Row("""{"__fusion_2_id":"P1"}""")));

        // act
        var first = client.GetOrCreateOperation(requests);
        var second = client.GetOrCreateOperation(requests);

        // assert
        Assert.Same(first, second);
    }

    private static SourceSchemaClientRequest Request(string operation, params JsonSegment[] rows)
    {
        var variables = ImmutableArray.CreateBuilder<VariableValues>(rows.Length);

        foreach (var row in rows)
        {
            variables.Add(new VariableValues(CompactPath.Root, row));
        }

        return new SourceSchemaClientRequest
        {
            Node = null!,
            SchemaName = "test",
            OperationType = OperationType.Query,
            OperationSourceText = operation,
            OperationHash = operation.ComputeHash(),
            Variables = variables.ToImmutable()
        };
    }

    private static JsonSegment Row(string json)
    {
        var writer = new ChunkedArrayWriter();
        var start = writer.Position;
        var bytes = Encoding.UTF8.GetBytes(json);
        writer.Write(bytes);
        return JsonSegment.Create(writer, start, bytes.Length);
    }

    [Fact]
    public async Task CreateClient_Should_ReturnAliasBatchingClient_When_AliasBatchingEnabled()
    {
        // arrange
        var schema = CreateCompositeSchema();
        var httpClientFactory = CreateHttpClientFactory();
        var aliasFactory = new AliasBatchingHttpSourceSchemaClientFactory(httpClientFactory);
        var configuration = new HttpSourceSchemaClientConfiguration(
            "a",
            new Uri("http://localhost:5000/graphql"),
            aliasBatching: true);

        // act
        var canHandle = aliasFactory.CanHandle(configuration);
        await using var client = aliasFactory.CreateClient(schema, configuration);

        // assert
        Assert.True(canHandle);
        Assert.IsType<AliasBatchingHttpSourceSchemaClient>(client);
        Assert.Equal(SourceSchemaClientCapabilities.AliasBatching, client.Capabilities);
    }

    [Fact]
    public void CanHandle_Should_ReturnFalse_When_AliasBatchingDisabled()
    {
        // arrange
        var httpClientFactory = CreateHttpClientFactory();
        var aliasFactory = new AliasBatchingHttpSourceSchemaClientFactory(httpClientFactory);
        var configuration = new HttpSourceSchemaClientConfiguration(
            "a",
            new Uri("http://localhost:5000/graphql"),
            aliasBatching: false);

        // act
        var canHandle = aliasFactory.CanHandle(configuration);

        // assert
        Assert.False(canHandle);
    }

    [Fact]
    public async Task CreateClient_Should_ReturnHttpClient_When_AliasBatchingDisabled()
    {
        // arrange
        var schema = CreateCompositeSchema();
        var httpClientFactory = CreateHttpClientFactory();
        var httpFactory = new HttpSourceSchemaClientFactory(httpClientFactory);
        var configuration = new HttpSourceSchemaClientConfiguration(
            "a",
            new Uri("http://localhost:5000/graphql"),
            aliasBatching: false);

        // act
        var canHandle = httpFactory.CanHandle(configuration);
        await using var client = httpFactory.CreateClient(schema, configuration);

        // assert
        Assert.True(canHandle);
        Assert.IsType<HttpSourceSchemaClient>(client);
    }

    private static IHttpClientFactory CreateHttpClientFactory()
        => new ServiceCollection()
            .AddHttpClient()
            .BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>();
}
