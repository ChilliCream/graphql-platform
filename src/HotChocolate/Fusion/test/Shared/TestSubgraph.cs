using System.Diagnostics.CodeAnalysis;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Shared;

public record TestSubgraph(
    TestServer TestServer,
    ISchema Schema,
    SubgraphTestContext Context,
    [StringSyntax("graphql")] string SchemaExtensions = "",
    bool IsOffline = false)
{
    public static Task<TestSubgraph> CreateAsync(
        [StringSyntax("graphql")] string schemaText,
        [StringSyntax("graphql")] string extensions = "",
        bool isOffline = false,
        bool enableSemanticNonNull = false)
        => CreateAsync(
            configure: builder => builder
                .AddDocumentFromString(schemaText)
                .AddResolverMocking()
                .AddTestDirectives(),
            extensions: extensions,
            isOffline: isOffline,
            enableSemanticNonNull: enableSemanticNonNull);

    public static async Task<TestSubgraph> CreateAsync(
        Action<IRequestExecutorBuilder> configure,
        [StringSyntax("graphql")] string extensions = "",
        bool isOffline = false,
        bool enableSemanticNonNull = false)
    {
        var testServerFactory = new TestServerFactory();
        var testContext = new SubgraphTestContext();

        var testServer = testServerFactory.Create(
            services =>
            {
                var builder = services
                    .AddRouting()
                    .AddGraphQLServer(disableDefaultSecurity: true);

                if (enableSemanticNonNull)
                {
                    builder.ModifyOptions(o => o.EnableSemanticNonNull = true);
                }

                configure(builder);
            },
            app =>
            {
                app.Use(next => context =>
                {
                    testContext.HasReceivedRequest = true;
                    return next(context);
                });

                app.UseRouting().UseEndpoints(endpoints => endpoints.MapGraphQL());
            });

        var schema = await testServer.Services.GetSchemaAsync();

        return new TestSubgraph(testServer, schema, testContext, extensions, isOffline);
    }

    public bool HasReceivedRequest => Context.HasReceivedRequest;
}

public class SubgraphTestContext
{
    public bool HasReceivedRequest { get; set; }
}
