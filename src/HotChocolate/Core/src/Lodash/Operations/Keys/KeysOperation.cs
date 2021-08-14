using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HotChocolate.Lodash
{
    public class KeysOperation : AggregationOperation
    {
        public override bool Rewrite(JsonNode? node, out JsonNode? rewritten)
        {
            if (node is null)
            {
                rewritten = null;
                return false;
            }

            if (node is JsonValue)
            {
                throw ThrowHelper.ExpectObjectButReceivedScalar(node.GetPath());
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

                rewritten = result;
                return true;
            }

            throw new ArgumentOutOfRangeException(nameof(node));
        }
    }
}
