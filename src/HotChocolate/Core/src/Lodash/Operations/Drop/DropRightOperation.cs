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

                int count;
                int i;
                for (count = Count - 1, i = arr.Count - 1; count >= 0; count--, i--)
                {
                    arr.RemoveAt(i);
                }

                return arr;
            }

            throw ThrowHelper.ExpectArrayButReceivedObject(node.GetPath());
        }
    }
}
