using System.Text.Json.Nodes;

namespace HotChocolate.Lodash
{
    public class ChunkOperation : AggregationOperation
    {
        public ChunkOperation(int count)
        {
            Count = count;
        }

        public int Count { get; }

        public override bool Rewrite(JsonNode? node, out JsonNode? rewritten)
        {
            rewritten = null;

            if (node is null)
            {
                return false;
            }

            if (Count < 1)
            {
                throw ThrowHelper.ChunkCountCannotBeLowerThanOne(node.GetPath());
            }

            if (node is JsonArray arr)
            {
                JsonArray chunks = new();
                JsonArray? currentChunk = null;

                while (arr.Count > 0)
                {
                    currentChunk ??= new JsonArray();

                    JsonNode? element = arr[0];
                    arr.RemoveAt(0);
                    currentChunk.Add(element);

                    if (currentChunk.Count == Count)
                    {
                        chunks.Add(currentChunk);
                        currentChunk = null;
                    }
                }

                if (currentChunk is not null)
                {
                    chunks.Add(currentChunk);
                }

                rewritten = chunks;
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
