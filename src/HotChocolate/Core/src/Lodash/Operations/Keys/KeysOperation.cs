using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HotChocolate.Lodash
{
    public class KeysOperation : AggregationOperation
    {
        public override JsonNode? Rewrite(JsonNode? node)
        {
            if (node is null)
            {
                return null;
            }

            if (node is JsonArray)
            {
                throw ThrowHelper.ExpectObjectButReceivedArray(node.GetPath());
            }

            if (node is JsonObject obj)
            {
                JsonArray result = new();

                foreach (KeyValuePair<string, JsonNode?> pair in obj)
                {
                    result.Add(pair.Key);
                }

                return result;
            }

            throw ThrowHelper.ExpectObjectButReceivedScalar(node.GetPath());
        }
    }
}
