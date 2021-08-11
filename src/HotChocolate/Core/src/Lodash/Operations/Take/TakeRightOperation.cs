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

        public override JsonNode? Rewrite(JsonNode? node)
        {
            if (node is null)
            {
                return null;
            }

            if (node is JsonArray arr)
            {
                JsonArray newArray = new();
                int count;
                int i;
                for (count = Count - 1, i = arr.Count - 1; count >= 0 && i >= 0; count--, i--)
                {
                    JsonNode? element = arr[i];
                    arr.RemoveAt(i);
                    newArray.Add(element);
                }

                return newArray;
            }

            throw ThrowHelper.ExpectArrayButReceivedObject(node.GetPath());
        }
    }
}
