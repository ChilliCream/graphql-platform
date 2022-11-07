using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Pipeline.PrepareDocuments;

public class SquashDocumentsMiddleware
{
    private readonly MergeSchema _next;

    public SquashDocumentsMiddleware(MergeSchema next)
    {
        _next = next;
    }

    public async ValueTask InvokeAsync(ISchemaMergeContext context)
    {
        var definitions = new List<IDefinitionNode>();

        foreach (Document document in context.Documents)
        {
            definitions.AddRange(document.SyntaxTree.Definitions);
        }

        context.Documents = context.Documents.Clear().Add(new("Merged", new(definitions)));

        await _next(context);
    }
}
