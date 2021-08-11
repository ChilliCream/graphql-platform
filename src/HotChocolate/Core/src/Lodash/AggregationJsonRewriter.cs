using System.Text.Json.Nodes;

namespace HotChocolate.Lodash
{
    public class AggregationJsonRewriter
    {
        public AggregationJsonRewriter(AggregationRewriterStep root)
        {
            Root = root;
        }

        public AggregationRewriterStep Root { get; }

        public JsonNode? Rewrite(JsonNode? node)
        {
            for (var i = 0; i < Root.Next.Count && node is not null; i++)
            {
                node = Root.Next[i].Rewrite(node);
            }

            for (var i = 0; i < Root.Operations.Count && node is not null; i++)
            {
                node = Root.Operations[i].Rewrite(node);
            }

            return node;
        }
    }
}
