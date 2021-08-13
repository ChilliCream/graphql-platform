using System.Text.Json.Nodes;

namespace HotChocolate.Lodash
{
    public class DropRightOperation : AggregationOperation
    {
        public DropRightOperation(int count)
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

            if (node is JsonArray arr)
            {
                if (Count > arr.Count)
                {
                    rewritten = new JsonArray();
                    return true;
                }

                int count;
                int i;
                for (count = Count - 1, i = arr.Count - 1; count >= 0; count--, i--)
                {
                    arr.RemoveAt(i);
                }

                rewritten = arr;
                return true;
            }

            if (node is JsonObject)
            {
                throw ThrowHelper.ExpectArrayButReceivedObject(node.GetPath());
            }

            throw ThrowHelper.ExpectArrayButReceivedScalar(node.GetPath());
        }
    }
}
