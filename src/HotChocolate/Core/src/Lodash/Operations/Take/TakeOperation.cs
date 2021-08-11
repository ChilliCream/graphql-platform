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

        public override JsonNode? Rewrite(JsonNode? node)
        {
            if (node is null)
            {
                return null;
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

                return newArray;
            }

            throw ThrowHelper.ExpectArrayButReceivedObject(node.GetPath());
        }
    }
}
