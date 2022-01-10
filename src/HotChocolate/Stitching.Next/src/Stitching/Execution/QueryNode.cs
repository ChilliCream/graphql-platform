using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Execution;

internal class QueryNode
{
    public QueryNode(NameString source)
    {
        Source = source;
    }

    public NameString Source { get; }

    public DocumentNode? Document { get; set; }

    public List<QueryNode> Nodes { get; } = new List<QueryNode>();
}
