using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Planning.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;

namespace HotChocolate.Fusion;

public abstract class FusionTestBase
{
    protected static CompositeSchema CreateCompositeSchema()
    {
        var compositeSchemaDoc = Utf8GraphQLParser.Parse(FileResource.Open("fusion1.graphql"));
        return CompositeSchemaBuilder.Create(compositeSchemaDoc);
    }

    protected static CompositeSchema CreateCompositeSchema(
        [StringSyntax("graphql")] string schema)
    {
        var compositeSchemaDoc = Utf8GraphQLParser.Parse(schema);
        return CompositeSchemaBuilder.Create(compositeSchemaDoc);
    }

    protected static RequestPlanNode PlanOperation(
        [StringSyntax("graphql")] string request,
        CompositeSchema compositeSchema)
    {
        var document = Utf8GraphQLParser.Parse(request);
        return PlanOperation(document, compositeSchema);
    }

    protected static RequestPlanNode PlanOperation(DocumentNode request, CompositeSchema compositeSchema)
    {
        var rewriter = new InlineFragmentOperationRewriter(compositeSchema);
        var rewritten = rewriter.RewriteDocument(request, null);

        var planner = new OperationPlanner(compositeSchema);
        return planner.CreatePlan(rewritten, null);
    }
}
