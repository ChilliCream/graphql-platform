using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Lodash
{
    public class LodashChunkOperation : LodashOperation
    {
        public LodashChunkOperation(int count)
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

        public static readonly ILodashOperationFactory Factory =
            new LodashChunkOperationFactory();

        private class LodashChunkOperationFactory : ILodashOperationFactory
        {
            public bool TryCreateOperation(
                DirectiveNode directiveNode,
                [NotNullWhen(true)] out LodashOperation? operation)
            {
                ArgumentNode? map =
                    directiveNode.Arguments.FirstOrDefault(x => x.Name.Value == "chunk");

                if (map is not null && map.Value is IntValueNode intValueNode)
                {
                    operation = new LodashChunkOperation(intValueNode.ToInt32());
                    return true;
                }

                operation = null;
                return false;
            }
        }
    }
}
