using System.Text.Json.Nodes;

namespace HotChocolate.Lodash
{
    public class LodashJsonRewriter
    {
        public LodashJsonRewriter(LodashRewriterStep root)
        {
            Root = root;
        }

        public LodashRewriterStep Root { get; }

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
