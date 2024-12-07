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

    protected static RootPlanNode PlanOperation(DocumentNode request, CompositeSchema compositeSchema)
    {
        var rewriter = new InlineFragmentOperationRewriter(compositeSchema);
        var rewritten = rewriter.RewriteDocument(request, null);

        var planner = new OperationPlanner(compositeSchema);
        return planner.CreatePlan(rewritten, null);
    }

    protected static async Task MatchSnapshotAsync(DocumentNode request,RootPlanNode plan)
    {
        var snapshot = new Snapshot();
        snapshot.Add(request, "Request");
        snapshot.Add(plan, "Plan");

        await snapshot.MatchMarkdownAsync();
    }
}
