using HotChocolate.Data.Projections;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.MongoDb;

internal sealed class MongoDbProjectionVisitor : ProjectionVisitor<MongoDbProjectionVisitorContext>
{
    protected override ISelectionVisitorAction Visit(
        Selection selection,
        MongoDbProjectionVisitorContext context)
    {
        var requirements = selection.RequiredMemberPaths;

        if (!requirements.IsEmpty)
        {
            var parentPath = context.Path.Count == 0 ? null : context.GetPath();

            foreach (var requirement in requirements)
            {
                var path = parentPath is null ? requirement : parentPath + "." + requirement;
                context.Projections.Push(new MongoDbRequiredProjectionOperation(path));
            }
        }

        return base.Visit(selection, context);
    }
}
