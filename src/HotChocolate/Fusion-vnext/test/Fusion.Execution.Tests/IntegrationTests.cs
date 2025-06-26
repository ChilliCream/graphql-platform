using System.Text;
using HotChocolate.Buffers;
using HotChocolate.Types;
using HotChocolate.Execution;
using HotChocolate.Transport.Formatters;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.Fusion;

public class IntegrationTests : FusionTestBase
{
    [Fact]
    public async Task Foo()
    {
        // arrange
        var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType(
                d => d.Name("Query")
                    .Field("foo")
                    .Resolve("foo"))
        );

        var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType(
                d => d.Name("Query")
                    .Field("bar")
                    .Resolve("bar")));

        var schema1 = await server1.Services.GetSchemaAsync("A");

        var schema2 = await server2.Services.GetSchemaAsync("B");

        var schema = ComposeSchemaDocument(
            schema1.ToString(),
            schema2.ToString());

        var services =
            new ServiceCollection()
                .AddHttpClient("A", server1)
                .AddHttpClient("B", server2)
                .AddGraphQLGateway()
                .AddInMemoryConfiguration(schema)
                .AddHttpClientConfiguration("A", new Uri("http://localhost:5000/graphql"))
                .AddHttpClientConfiguration("B", new Uri("http://localhost:5000/graphql"))
                .Services
                .BuildServiceProvider();

        var executor = await services.GetRequestExecutorAsync();
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ foo bar }")
                .Build());

        using var buffer = new PooledArrayWriter();
        JsonResultFormatter.Indented.Format(result.ExpectOperationResult(), buffer);
        Encoding.UTF8.GetString(buffer.GetWrittenSpan()).MatchSnapshot();
    }
}

file static class Extensions
{
    public static IServiceCollection AddHttpClient(
        this IServiceCollection services,
        string name,
        TestServer server)
    {
        services.TryAddSingleton<IHttpClientFactory, Factory>();
        return services.AddSingleton(new TestServerRegistration(name, server));
    }

    private class Factory : IHttpClientFactory
    {
        private readonly Dictionary<string, TestServerRegistration> _registrations;

        public Factory(IEnumerable<TestServerRegistration> registrations)
        {
            _registrations = registrations.ToDictionary(r => r.Name, r => r);
        }

        public HttpClient CreateClient(string name)
        {
            if (_registrations.TryGetValue(name, out var registration))
            {
                return registration.Server.CreateClient();
            }

            throw new InvalidOperationException(
                $"No test server registered with the name: {name}");
        }
    }

    private record TestServerRegistration(string Name, TestServer Server);
}
