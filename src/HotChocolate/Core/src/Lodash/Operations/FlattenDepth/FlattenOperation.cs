using System.Text.Json.Nodes;

namespace HotChocolate.Lodash
{
    public class FlattenOperation : AggregationOperation
    {
        public FlattenOperation(int count)
        {
            Count = count;
        }

        public int Count { get; }

        public override bool Rewrite(JsonNode? node, out JsonNode? rewritten)
        {
            if (node is null)
            {
                rewritten = null;
                return false;
            }

            JsonArray result = new();

            void Flatten(JsonNode? innerNode, int depth)
            {
                if (innerNode is JsonArray arr)
                {
                    if (depth == Count)
                    {
                        while (arr.Count > 0)
                        {
                            JsonNode? element = arr[0];
                            arr.RemoveAt(0);
                            result.Add(element);
                        }
                    }
                    else
                    {
                        Flatten(arr, depth + 1);
                    }
                }
                else
                {
                    result.Add(innerNode);
                }
            }

            Flatten(node, 0);

            rewritten = result;
            return true;
        }
    }
}
