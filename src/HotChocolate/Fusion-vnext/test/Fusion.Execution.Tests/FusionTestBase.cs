using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Configuration;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
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
        var sourceSchemas = CreateSourceSchemaTexts(schemas);

        var compositionLog = new CompositionLog();
        var composer = new SchemaComposer(sourceSchemas, new SchemaComposerOptions(), compositionLog);
        var result = composer.Compose();

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        var compositeSchemaDoc = result.Value.ToSyntaxNode();
        return FusionSchemaDefinition.Create(compositeSchemaDoc);
    }

    protected static FusionSchemaDefinition ComposeSchema(params TestSourceSchema[] schemas)
        => ComposeSchema(schemas.Select(t => t.Schema).ToArray());

    protected static DocumentNode ComposeSchemaDocument(
        [StringSyntax("graphql")] params string[] schemas)
    {
        var sourceSchemas = CreateSourceSchemaTexts(schemas);

        var compositionLog = new CompositionLog();
        var composer = new SchemaComposer(sourceSchemas, new SchemaComposerOptions(), compositionLog);
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

    protected static OperationPlan PlanOperation(
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
        const string id = "123456789101112";
        return planner.CreatePlan(id, id, id, operation);
    }

    protected static void MatchInline(
        OperationPlan plan,
        [StringSyntax("yaml")] string expected)
    {
        var formatter = new YamlOperationPlanFormatter();
        var actual = formatter.Format(plan);
        actual.MatchInlineSnapshot(expected + Environment.NewLine);
    }

    protected static void MatchSnapshot(
        OperationPlan plan)
    {
        var formatter = new YamlOperationPlanFormatter();
        var actual = formatter.Format(plan);
        actual.MatchSnapshot(extension: ".yaml");
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

    private static List<SourceSchemaText> CreateSourceSchemaTexts(IEnumerable<string> schemas)
    {
        var sourceSchemas = new List<SourceSchemaText>();
        var autoName = 'a';

        foreach (var schema in schemas)
        {
            string name;
            string sourceText;

            var lines = schema.Split(["\r\n", "\n"], StringSplitOptions.None);

            if (lines.Length > 0 && lines[0].StartsWith("# name:"))
            {
                name = lines[0]["# name:".Length..].Trim();
                sourceText = string.Join(Environment.NewLine, lines.Skip(1));
            }
            else
            {
                name = autoName.ToString();
                autoName++;
                sourceText = schema;
            }

            sourceSchemas.Add(new SourceSchemaText(name, sourceText));
        }

        return sourceSchemas;
    }

    protected record TestSourceSchema([StringSyntax("graphql")] string Schema);
}
