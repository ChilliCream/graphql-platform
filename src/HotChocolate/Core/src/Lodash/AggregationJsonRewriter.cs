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
                if (Root.Next[i].Rewrite(node, out JsonNode? newNode))
                {
                    node = newNode;
                }
            }

            for (var i = 0; i < Root.Operations.Count && node is not null; i++)
            {
                if (Root.Operations[i].Rewrite(node, out JsonNode? newNode))
                {
                    node = newNode;
                }
            }

            return node;
        }
    }
}
