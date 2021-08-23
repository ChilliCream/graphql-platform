using System;
using System.Text.Json.Nodes;

namespace HotChocolate.Lodash
{
    public class TakeRightOperation : AggregationOperation
    {
        public TakeRightOperation(int count)
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

                JsonArray newArray = new();
                int startIndex = arr.Count - Count;
                while (newArray.Count < Count || arr.Count == 0)
                {
                    JsonNode? element = arr[startIndex];
                    arr.RemoveAt(startIndex);
                    newArray.Add(element);
                }

                rewritten = newArray;
                return true;
            }

            throw new ArgumentOutOfRangeException(nameof(node));
        }
    }
}
