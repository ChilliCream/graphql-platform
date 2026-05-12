#if NET9_0_OR_GREATER
using HotChocolate.Adapters.OpenApi;
using HotChocolate.Adapters.OpenApi.Storage;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Diagnostics.ActivityTestHelper;

namespace HotChocolate.Diagnostics;

[Collection("Instrumentation")]
public class OpenApiAdapterActivityTests
{
    [Fact]
    public async Task Http_Get_OpenApi_Field_Does_Not_Exist()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateServer(
                """
                query GetMissing @http(method: GET, route: "/invalid-graphql-query") {
                  doesNotExist
                }
                """);
            using var client = server.CreateClient();

            // act
            using var response = await client.GetAsync("/invalid-graphql-query");
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
            using var server = CreateServer(
                """
                query GetBook @http(method: GET, route: "/book") {
                  book {
                    title
                  }
                }
                """);
            using var client = server.CreateClient();

            // act
            using var response = await client.GetAsync("/does-not-exist");
            await response.Content.ReadAsStringAsync();

            // assert
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Http_Get_OpenApi()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateServer(
                """
                query GetBook @http(method: GET, route: "/book") {
                  book {
                    title
                  }
                }
                """);
            using var client = server.CreateClient();

            // act
            using var response = await client.GetAsync("/book");
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
            using var server = CreateServer(
                """
                query GetFaultyBook @http(method: GET, route: "/faulty-book") {
                  faultyBook {
                    title
                  }
                }
                """);
            using var client = server.CreateClient();

            // act
            using var response = await client.GetAsync("/faulty-book");
            await response.Content.ReadAsStringAsync();

            // assert
            activities.MatchSnapshot();
        }
    }

    private static TestServer CreateServer(params string[] documents)
    {
        var storage = new InMemoryOpenApiDefinitionStorage(documents);

        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services
                    .AddGraphQLServer()
                    .AddInstrumentation()
                    .AddOpenApi()
                    .AddOpenApiDefinitionStorage(storage)
                    .AddQueryType<Query>();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapOpenApiEndpoints());
            });

        return new TestServer(builder);
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
