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

        public override JsonNode? Rewrite(JsonNode? node)
        {
            if (node is null)
            {
                return null;
            }

            if (node is JsonArray arr)
            {
                var chunks = new JsonArray();
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

                return chunks;
            }

            throw ThrowHelper.ExpectArrayButReceivedObject(node.GetPath());
        }
    }
}
