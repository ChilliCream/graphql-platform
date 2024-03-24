using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Shared;

public record TestSubgraph(string SubgraphName, TestServer TestServer, ISchema Schema, string SchemaExtensions = "")
{
    public static async Task<TestSubgraph> CreateAsync(
        string subgraphName,
        Action<IRequestExecutorBuilder> configureBuilder,
        string extensions = "")
    {
        var testServerFactory = new TestServerFactory();

        var testServer = testServerFactory.Create(
            services =>
            {
                var builder = services
                    .AddRouting()
                    .AddGraphQLServer();

                configureBuilder(builder);
            },
            app =>
            {
                app.UseRouting().UseEndpoints(endpoints => endpoints.MapGraphQL());
            });

        var schema = await testServer.Services.GetSchemaAsync();

        return new TestSubgraph(subgraphName, testServer, schema, extensions);
    }
}
