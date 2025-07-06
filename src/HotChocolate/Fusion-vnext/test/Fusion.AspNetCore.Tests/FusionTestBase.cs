using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion;

public abstract class FusionTestBase : IDisposable
{
    private readonly TestServerSession _testServerSession = new();
    private bool _disposed;

    protected static FusionSchemaDefinition CreateCompositeSchema()
    {
        var compositeSchemaDoc = Utf8GraphQLParser.Parse(FileResource.Open("fusion1.graphql"));
        return FusionSchemaDefinition.Create(compositeSchemaDoc);
    }

    protected static FusionSchemaDefinition CreateCompositeSchema(
        [StringSyntax("graphql")] string schema)
    {
        var compositeSchemaDoc = Utf8GraphQLParser.Parse(schema);
        return FusionSchemaDefinition.Create(compositeSchemaDoc);
    }

    protected static FusionSchemaDefinition ComposeSchema(
        [StringSyntax("graphql")] params string[] schemas)
    {
        var compositionLog = new CompositionLog();
        var composer = new SchemaComposer(schemas, compositionLog);
        var result = composer.Compose();

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        var compositeSchemaDoc = result.Value.ToSyntaxNode();
        return FusionSchemaDefinition.Create(compositeSchemaDoc);
    }

    protected static DocumentNode ComposeSchemaDocument(
        [StringSyntax("graphql")] params string[] schemas)
    {
        var compositionLog = new CompositionLog();
        var composer = new SchemaComposer(schemas, compositionLog);
        var result = composer.Compose();

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        return result.Value.ToSyntaxNode();
    }

    public TestServer CreateSourceSchema(
        string schemaName,
        Action<IRequestExecutorBuilder> configureBuilder,
        Action<IServiceCollection>? configureServices = null,
        Action<IApplicationBuilder>? configureApplication = null)
    {
        configureApplication ??=
            app =>
            {
                app.UseWebSockets();
                app.UseRouting();
                app.UseEndpoints(endpoint => endpoint.MapGraphQL(schemaName: schemaName));
            };

        return _testServerSession.CreateServer(
            services =>
            {
                services.AddRouting();
                var builder = services.AddGraphQLServer(schemaName);
                configureBuilder(builder);
                configureServices?.Invoke(services);
            },
            configureApplication);
    }

    public async Task<TestServer> CreateCompositeSchemaAsync(
        (string SchemaName, TestServer Server)[] sourceSchemaServers,
        Action<IServiceCollection>? configureServices = null,
        Action<IApplicationBuilder>? configureApplication = null)
    {
        var sourceSchemas = new List<string>();
        var gatewayServices = new ServiceCollection();
        var gatewayBuilder = gatewayServices.AddGraphQLGatewayServer();

        foreach (var (name, server) in sourceSchemaServers)
        {
            var schemaDocument = await server.Services.GetSchemaAsync(name);
            sourceSchemas.Add(schemaDocument.ToString());
            gatewayServices.AddHttpClient(name, server);
            gatewayBuilder.AddHttpClientConfiguration(name, new Uri("http://localhost:5000/graphql"));
        }

        var compositionLog = new CompositionLog();
        var composer = new SchemaComposer(sourceSchemas, compositionLog);
        var result = composer.Compose();

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        gatewayBuilder.AddInMemoryConfiguration(result.Value.ToSyntaxNode());

        configureApplication ??=
            app =>
            {
                app.UseWebSockets();
                app.UseRouting();
                app.UseEndpoints(endpoint => endpoint.MapGraphQL());
            };

        return _testServerSession.CreateServer(
            services =>
            {
                services.AddRouting();

                foreach (var serviceDescriptor in gatewayServices)
                {
                    services.Add(serviceDescriptor);
                }

                configureServices?.Invoke(services);
            },
            configureApplication);
    }

    protected static OperationExecutionPlan PlanOperation(
        FusionSchemaDefinition schema,
        [StringSyntax("graphql")] string operationText)
    {
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());

        var operationDoc = Utf8GraphQLParser.Parse(operationText);

        var rewriter = new InlineFragmentOperationRewriter(schema);
        var rewritten = rewriter.RewriteDocument(operationDoc, operationName: null);
        var operation = rewritten.Definitions.OfType<OperationDefinitionNode>().First();

        var compiler = new OperationCompiler(schema, pool);
        var planner = new OperationPlanner(schema, compiler);
        return planner.CreatePlan("123", operation);
    }

    protected static void MatchInline(
        OperationExecutionPlan plan,
        [StringSyntax("yaml")] string expected)
    {
        var formatter = new YamlExecutionPlanFormatter();
        var actual = formatter.Format(plan);
        actual.MatchInlineSnapshot(expected + Environment.NewLine);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _testServerSession.Dispose();
        }
    }
}
