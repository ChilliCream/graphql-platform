#if NET9_0_OR_GREATER
using HotChocolate.Adapters.OpenApi;
using HotChocolate.Adapters.OpenApi.Storage;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Fusion.Diagnostics.ActivityTestHelper;

namespace HotChocolate.Fusion.Diagnostics;

[Collection("Instrumentation")]
public class FusionOpenApiAdapterActivityTests : FusionTestBase
{
    [Fact]
    public async Task Http_Get_OpenApi_Field_Does_Not_Exist()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateSourceSchema("a", b => b.AddQueryType<Query>());

            using var gateway = await CreateGatewayAsync(
                server,
                """
                query GetMissing @http(method: GET, route: "/invalid-graphql-query") {
                  doesNotExist
                }
                """);
            using var client = gateway.CreateClient();

            // act
            using var response = await client.GetAsync("/invalid-graphql-query");
            await response.Content.ReadAsStringAsync();

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Http_Get_OpenApi()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateSourceSchema("a", b => b.AddQueryType<Query>());

            using var gateway = await CreateGatewayAsync(
                server,
                """
                query GetBook @http(method: GET, route: "/book") {
                  book {
                    title
                  }
                }
                """);
            using var client = gateway.CreateClient();

            // act
            using var response = await client.GetAsync("/book");
            await response.Content.ReadAsStringAsync();

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Http_Get_OpenApi_Endpoint_Does_Not_Exist()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateSourceSchema("a", b => b.AddQueryType<Query>());

            using var gateway = await CreateGatewayAsync(
                server,
                """
                query GetBook @http(method: GET, route: "/book") {
                  book {
                    title
                  }
                }
                """);
            using var client = gateway.CreateClient();

            // act
            using var response = await client.GetAsync("/does-not-exist");
            await response.Content.ReadAsStringAsync();

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Http_Get_OpenApi_GraphQL_Field_Error()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateSourceSchema("a", b => b.AddQueryType<Query>());

            using var gateway = await CreateGatewayAsync(
                server,
                """
                query GetFaultyBook @http(method: GET, route: "/faulty-book") {
                  faultyBook {
                    title
                  }
                }
                """);
            using var client = gateway.CreateClient();

            // act
            using var response = await client.GetAsync("/faulty-book");
            await response.Content.ReadAsStringAsync();

            // assert
            activities.MatchSnapshot();
        }
    }

    private async Task<Gateway> CreateGatewayAsync(
        TestServer sourceSchema,
        params string[] documents)
    {
        var storage = new InMemoryOpenApiDefinitionStorage(documents);

        return await CreateCompositeSchemaAsync(
            [("a", sourceSchema)],
            configureApplication: app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapOpenApiEndpoints());
            },
            configureGatewayBuilder: b => b
                .AddInstrumentation()
                .AddOpenApi()
                .AddOpenApiDefinitionStorage(storage));
    }

    public class Query
    {
        public Book GetBook() => new("C# in Depth");

        public Book GetFaultyBook(IResolverContext context)
            => throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Something went wrong")
                    .SetPath(context.Path)
                    .Build());
    }

    public record Book(string Title);

    private sealed class InMemoryOpenApiDefinitionStorage(IEnumerable<string> documents)
        : IOpenApiDefinitionStorage
    {
        private readonly List<IOpenApiDefinition> _definitions = documents
            .Select(d => OpenApiDefinitionParser.Parse(Utf8GraphQLParser.Parse(d)))
            .ToList();

        public ValueTask<IEnumerable<IOpenApiDefinition>> GetDefinitionsAsync(
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult<IEnumerable<IOpenApiDefinition>>(_definitions);

        public IDisposable Subscribe(IObserver<OpenApiDefinitionStorageEventArgs> observer)
            => NoopDisposable.Instance;

        private sealed class NoopDisposable : IDisposable
        {
            public static readonly NoopDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
#endif
