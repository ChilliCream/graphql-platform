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

            if (node is JsonArray arr)
            {
                JsonArray chunks = new();
                JsonArray currentChunk = null!;

                int index = 0;
                int chunkIndex = 0;
                for (var i = 0; i < arr.Count; i++)
                {
                    JsonNode? element = arr[i];
                    arr.RemoveAt(i--);

                    if (chunkIndex == Count)
                    {
                        index++;
                        chunkIndex = 0;
                    }

                    if (chunks.Count <= index)
                    {
                        currentChunk = new JsonArray();
                        chunks.Add(currentChunk);
                    }

                    currentChunk.Add(element);
                    chunkIndex++;
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
