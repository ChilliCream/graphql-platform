using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Shared;

public record TestSubgraph(TestServer TestServer, ISchema Schema, string SchemaExtensions = "", bool IsOffline = false)
{
    public static async Task<TestSubgraph> CreateAsync(
        string schemaText,
        bool isOffline = false)
    {
        var testServerFactory = new TestServerFactory();

        var testServer = testServerFactory.Create(
            services =>
            {
                services
                    .AddRouting()
                    .AddGraphQLServer()
                    .AddDocumentFromString(schemaText)
                    .AddResolverMocking()
                    .AddTestDirectives();
            },
            app =>
            {
                app.UseRouting().UseEndpoints(endpoints => endpoints.MapGraphQL());
            });

        var schema = await testServer.Services.GetSchemaAsync();

        return new TestSubgraph(testServer, schema, IsOffline: isOffline);
    }

    public static async Task<TestSubgraph> CreateAsync(
        Action<IRequestExecutorBuilder> configureBuilder,
        string extensions = "",
        bool isOffline = false)
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

        return new TestSubgraph(testServer, schema, extensions, isOffline);
    }
}
