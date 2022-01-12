using System.Collections.Generic;
using System.Text;
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

    public override string ToString() => ToString(0);

    private string ToString(int indent)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Source);
        sb.AppendLine();

        if (Document is not null)
        {
            sb.AppendLine(Document.ToString());
        }

        if (Nodes.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine(new string('-', indent * 2 + 2) + ">");
            var next = false;

            foreach (QueryNode node in Nodes)
            {
                if (next)
                {
                    sb.AppendLine();
                }

                next = true;
                sb.AppendLine(node.ToString(indent + 1));
            }

            sb.AppendLine("<" + new string('-', indent * 2 + 2));
        }

        return sb.ToString();
    }
}
