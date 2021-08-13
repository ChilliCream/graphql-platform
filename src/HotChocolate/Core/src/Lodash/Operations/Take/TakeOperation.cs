using System.Text.Json.Nodes;

namespace HotChocolate.Lodash
{
    public class TakeOperation : AggregationOperation
    {
        public TakeOperation(int count)
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
                var initialCount = arr.Count;
                var newArray = new JsonArray();
                for (var count = 0; count < Count && count < initialCount; count++)
                {
                    JsonNode? element = arr[0];
                    arr.RemoveAt(0);
                    newArray.Add(element);
                }

                rewritten = newArray;
                return true;
            }

            throw ThrowHelper.ExpectArrayButReceivedObject(node.GetPath());
        }
    }
}
