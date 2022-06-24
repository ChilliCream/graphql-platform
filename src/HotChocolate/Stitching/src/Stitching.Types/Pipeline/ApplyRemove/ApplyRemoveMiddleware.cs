using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Pipeline.ApplyRemove;

/// <summary>
/// Removes Types and Fields which have the remove directive associated
/// </summary>
public sealed class ApplyRemoveMiddleware
{
    private readonly RemoveIndexer _indexer = new();
    private readonly RemoveRewriter _rewriter = new();
    private readonly MergeSchema _next;

    public ApplyRemoveMiddleware(MergeSchema next)
    {
        _next = next;
    }

    public async ValueTask InvokeAsync(ISchemaMergeContext context)
    {
        for (var i = 0; i < context.Documents.Count; i++)
        {
            Document doc = context.Documents[i];

            var removeContext = new RemoveContext(doc.Name);
            _indexer.Visit(doc.SyntaxTree, removeContext);

            if (removeContext.RemovedFields.Count > 0)
            {
                foreach ((var coordinate, var renameInfo) in removeContext.RemovedFields.ToArray())
                {
                    var possibleInterface = coordinate.Name.Value;
                    if (!removeContext.ImplementedBy.TryGetValue(possibleInterface, out var types))
                    {
                        continue;
                    }

                    foreach (var implementor in types)
                    {
                        var implCoordinate = coordinate.WithName(new(implementor));
                        removeContext.RemovedFields[implCoordinate] = renameInfo;
                    }
                }

                foreach (var coordinate in removeContext.RemovedFields.Keys)
                {
                    removeContext.TypesWithRemovedFields.Add(coordinate.Name.Value);
                }
            }

            var syntaxTree = (DocumentNode?) _rewriter.Rewrite(doc.SyntaxTree, removeContext);
            if (ReferenceEquals(doc.SyntaxTree, syntaxTree))
            {
                continue;
            }

            if (syntaxTree is not null)
            {
                context.Documents = context.Documents.SetItem(i, new Document(doc.Name, syntaxTree));
            }
            else
            {
                context.Documents = context.Documents.RemoveAt(i);
            }
        }

        await _next(context);
    }
}
