using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion;

public abstract class FusionTestBase
{
    protected static FusionSchemaDefinition CreateSchema()
    {
        var compositeSchemaDoc = Utf8GraphQLParser.Parse(FileResource.Open("fusion1.graphql"));
        return FusionSchemaDefinition.Create(compositeSchemaDoc);
    }

    protected static FusionSchemaDefinition CreateSchema(
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
}
