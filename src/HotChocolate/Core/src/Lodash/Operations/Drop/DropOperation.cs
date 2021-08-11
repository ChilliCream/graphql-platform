using System.Text.Json.Nodes;

namespace HotChocolate.Lodash
{
    public class DropOperation : AggregationOperation
    {
        public DropOperation(int count)
        {
            Count = count;
        }

        public int Count { get; }

        public override JsonNode? Rewrite(JsonNode? node)
        {
            if (node is null)
            {
                return null;
            }

            if (node is JsonArray arr)
            {
                if (Count > arr.Count)
                {
                    return new JsonArray();
                }

                for (var count = Count - 1; count >= 0; count--)
                {
                    arr.RemoveAt(0);
                }

                return arr;
            }

            throw ThrowHelper.ExpectArrayButReceivedObject(node.GetPath());
        }
    }
}
