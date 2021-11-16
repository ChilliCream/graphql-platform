using System.Text.Json.Nodes;

namespace HotChocolate.Lodash
{
    public class FlattenOperation : AggregationOperation
    {
        public FlattenOperation(int depth)
        {
            Depth = depth;
        }

        public int Depth { get; }

        public override bool Rewrite(JsonNode? node, out JsonNode? rewritten)
        {
            if (node is null)
            {
                rewritten = null;
                return false;
            }

            if (Depth < 1)
            {
                throw ThrowHelper.FlattenDepthCannotBeLowerThanOne(node.GetPath());
            }

            JsonArray result = new();
            Flatten(node, 0, result);
            rewritten = result;

            return true;
        }

        private void Flatten(JsonNode? innerNode, int currentDepth, JsonArray result)
        {
            if (innerNode is JsonArray arr && currentDepth <= Depth)
            {
                while (arr.Count > 0)
                {
                    JsonNode? element = arr[0];
                    arr.RemoveAt(0);
                    Flatten(element, currentDepth + 1, result);
                }
            }
            else
            {
                result.Add(innerNode);
            }
        }
    }
}
