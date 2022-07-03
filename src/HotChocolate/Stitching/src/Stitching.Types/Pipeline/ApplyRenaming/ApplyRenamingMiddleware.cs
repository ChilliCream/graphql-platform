using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Pipeline.ApplyRenaming;

public sealed class ApplyRenamingMiddleware
{
    private readonly RenameIndexer _indexer = new();
    private readonly RenameRewriter _rewriter = new();
    private readonly MergeSchema _next;

    public ApplyRenamingMiddleware(MergeSchema next)
    {
        _next = next;
    }

    public async ValueTask InvokeAsync(ISchemaMergeContext context)
    {
        for (var i = 0; i < context.Documents.Count; i++)
        {
            Document doc = context.Documents[i];

            var renameContext = new RenameContext(doc.Name);
            _indexer.Visit(doc.SyntaxTree, renameContext);

            if (renameContext.RenamedFields.Count > 0)
            {
                foreach ((var coordinate, var renameInfo) in renameContext.RenamedFields.ToArray())
                {
                    var possibleInterface = coordinate.Name.Value;
                    if (renameContext.ImplementedBy.TryGetValue(possibleInterface, out var types))
                    {
                        foreach (var implementor in types)
                        {
                            var implCoordinate = coordinate.WithName(new(implementor));
                            renameContext.RenamedFields[implCoordinate] = renameInfo;
                        }
                    }
                }

                foreach (var coordinate in renameContext.RenamedFields.Keys)
                {
                    renameContext.TypesWithFieldRenames.Add(coordinate.Name.Value);
                }
            }

            var syntaxTree = (DocumentNode)_rewriter.Rewrite(doc.SyntaxTree, renameContext);
            context.Documents = context.Documents.SetItem(i, new Document(doc.Name, syntaxTree));
        }

        await _next(context);
    }
}
