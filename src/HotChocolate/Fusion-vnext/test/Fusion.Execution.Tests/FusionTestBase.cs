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

    protected static RequestPlanNode PlanOperationAsync(CompositeSchema compositeSchema, string operation)
    {
        var doc = Utf8GraphQLParser.Parse(operation);

        var rewriter = new InlineFragmentOperationRewriter(compositeSchema);
        var rewritten = rewriter.RewriteDocument(doc, null);

        var planner = new OperationPlanner(compositeSchema);
        return planner.CreatePlan(rewritten, null);
    }
}
