using System;
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

            if (node is JsonObject)
            {
                throw ThrowHelper.ExpectArrayButReceivedObject(node.GetPath());
            }

            if (node is JsonValue)
            {
                throw ThrowHelper.ExpectArrayButReceivedScalar(node.GetPath());
            }

            if (node is JsonArray arr)
            {
                if (Count > arr.Count)
                {
                    rewritten = arr;
                    return true;
                }

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

            throw new ArgumentOutOfRangeException(nameof(node));
        }
    }
}
