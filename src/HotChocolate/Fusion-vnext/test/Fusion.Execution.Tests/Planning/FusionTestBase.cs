using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Definitions;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.Fusion;

public abstract class FusionTestBase
{
    protected static FusionSchemaDefinition CreateSchema()
    {
        var compositeSchemaDoc = Utf8GraphQLParser.Parse(FileResource.Open("fusion1.graphql"));
        return CompositeSchemaBuilder.Create(compositeSchemaDoc);
    }

    protected static FusionSchemaDefinition CreateSchema(
        [StringSyntax("graphql")] string schema)
    {
        var compositeSchemaDoc = Utf8GraphQLParser.Parse(schema);
        return CompositeSchemaBuilder.Create(compositeSchemaDoc);
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
        return CompositeSchemaBuilder.Create(compositeSchemaDoc);
    }

    protected static ExecutionPlan PlanOperation(
        FusionSchemaDefinition schema,
        [StringSyntax("graphql")] string operationText)
    {
        var operationDoc = Utf8GraphQLParser.Parse(operationText);

        var rewriter = new InlineFragmentOperationRewriter(schema);
        var rewritten = rewriter.RewriteDocument(operationDoc, null);
        var operation = rewritten.Definitions.OfType<OperationDefinitionNode>().First();

        var planner = new OperationPlanner(schema);
        return planner.CreatePlan(operation);
    }

    protected static void MatchInline(
        ExecutionPlan plan,
        [StringSyntax("yaml")] string expected)
    {
        var formatter = new YamlExecutionPlanFormatter();
        var actual = formatter.Format(plan);
        actual.MatchInlineSnapshot(expected + Environment.NewLine);
    }

    protected record TestSubgraph([StringSyntax("graphql")] string Schema, bool Preprocess = true);

    protected class TestSubgraphCollection(params TestSubgraph[] subgraphs)
    {
        public FusionSchemaDefinition BuildFusionSchema()
        {
            var compositionLog = new CompositionLog();
            var schemas = subgraphs.Select(GetSchemaFromSubgraph);
            var composer = new SchemaComposer(schemas, compositionLog);
            var result = composer.Compose();

            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(result.Errors[0].Message);
            }

            var compositeSchemaDoc = result.Value.ToSyntaxNode();
            return CompositeSchemaBuilder.Create(compositeSchemaDoc);
        }

        private static string GetSchemaFromSubgraph(TestSubgraph subgraph, int index)
        {
            var schema = SchemaParser.Parse(subgraph.Schema);

            if (subgraph.Preprocess)
            {
                schema = new SourceSchemaPreprocessor(schema).Process();
            }

            var schemaNameDirective = schema.Directives.FirstOrDefault(WellKnownDirectiveNames.SchemaName);

            if (schemaNameDirective is null)
            {
                var subgraphName = $"Subgraph_{index + 1}";
                var stringType = BuiltIns.String.Create();
                var schemaNameDirectiveDefinition = new SchemaNameMutableDirectiveDefinition(stringType);

                schema.DirectiveDefinitions.Add(schemaNameDirectiveDefinition);

                schema.Directives.Add(new Directive(schemaNameDirectiveDefinition,
                    [new ArgumentAssignment(WellKnownArgumentNames.Value, subgraphName)]));
            }

            return schema.ToString();
        }
    }
}
