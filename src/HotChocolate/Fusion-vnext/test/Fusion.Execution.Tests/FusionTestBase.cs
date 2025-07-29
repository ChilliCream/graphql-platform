using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Configuration;
using HotChocolate.Fusion.Definitions;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Directive = HotChocolate.Types.Mutable.Directive;

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
        var composer = new SchemaComposer(schemas, new SchemaComposerOptions(), compositionLog);
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
        var composer = new SchemaComposer(schemas, new SchemaComposerOptions(), compositionLog);
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

    protected static OperationExecutionPlan PlanOperation(
        FusionSchemaDefinition schema,
        [StringSyntax("graphql")] string operationText)
    {
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());

        var operationDoc = Utf8GraphQLParser.Parse(operationText);

        var rewriter = new InlineFragmentOperationRewriter(schema);
        var rewritten = rewriter.RewriteDocument(operationDoc, null);
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

    protected static void MatchSnapshot(
        OperationExecutionPlan plan)
    {
        var formatter = new YamlExecutionPlanFormatter();
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

    protected record TestSubgraph([StringSyntax("graphql")] string Schema);

    protected class TestSubgraphCollection(params TestSubgraph[] subgraphs)
    {
        public FusionSchemaDefinition BuildFusionSchema()
        {
            var schemas = subgraphs.Select(s => s.Schema).ToArray();
            var rewrittenSchemas = new string[schemas.Length];

            for (var i = 0; i < schemas.Length; i++)
            {
                var sourceSchema = SchemaParser.Parse(schemas[i]);

                AddFusionDirectives(sourceSchema);

                if (sourceSchema.Name == "default")
                {
                    sourceSchema.Name = $"Subgraph_{i + 1}";

                    if (sourceSchema.DirectiveDefinitions.TryGetDirective(WellKnownDirectiveNames.SchemaName,
                        out var schemaNameDirectiveDefinition))
                    {
                        sourceSchema.Directives.Add(new Directive(schemaNameDirectiveDefinition,
                            new ArgumentAssignment(WellKnownArgumentNames.Value, sourceSchema.Name)));
                    }
                }

                rewrittenSchemas[i] = sourceSchema.ToString();
            }

            return ComposeSchema(rewrittenSchemas);
        }

        private static void AddFusionDirectives(MutableSchemaDefinition schema)
        {
            var fieldSelectionMapType = MutableScalarTypeDefinition.Create(WellKnownTypeNames.FieldSelectionMap);
            var fieldSelectionSetType = MutableScalarTypeDefinition.Create(WellKnownTypeNames.FieldSelectionSet);
            var stringType = BuiltIns.String.Create();

            var fusionDirectives = new Dictionary<string, MutableDirectiveDefinition>()
            {
                { "external", new ExternalMutableDirectiveDefinition() },
                { "inaccessible", new InaccessibleMutableDirectiveDefinition() },
                { "internal", new InternalMutableDirectiveDefinition() },
                { "is", new IsMutableDirectiveDefinition(fieldSelectionMapType) },
                { "key", new KeyMutableDirectiveDefinition(fieldSelectionSetType) },
                { "lookup", new LookupMutableDirectiveDefinition() },
                { "override", new OverrideMutableDirectiveDefinition(stringType) },
                { "provides", new ProvidesMutableDirectiveDefinition(fieldSelectionSetType) },
                { "require", new RequireMutableDirectiveDefinition(fieldSelectionMapType) },
                { "schemaName", new SchemaNameMutableDirectiveDefinition(stringType) },
                { "shareable", new ShareableMutableDirectiveDefinition() }
            };

            if (schema.Types.TryGetType(fieldSelectionMapType.Name, out var existingFieldSelectionMapType))
            {
                schema.Types.Remove(existingFieldSelectionMapType);
            }

            schema.Types.Add(fieldSelectionMapType);

            if (schema.Types.TryGetType(fieldSelectionSetType.Name, out var existingFieldSelectionSetType))
            {
                schema.Types.Remove(existingFieldSelectionSetType);
            }

            schema.Types.Add(fieldSelectionSetType);

            foreach (var fusionDirective in fusionDirectives.Values)
            {
                if (schema.DirectiveDefinitions.TryGetDirective(fusionDirective.Name,
                    out var existingDirectiveDefinition))
                {
                    schema.DirectiveDefinitions.Remove(existingDirectiveDefinition);
                }

                schema.DirectiveDefinitions.Add(fusionDirective);
            }
        }
    }
}
