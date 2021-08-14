using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HotChocolate.Lodash
{
    public class GroupByOperation : AggregationOperation
    {
        public GroupByOperation(string key)
        {
            Key = key;
        }

        public string Key { get; }

        public override bool Rewrite(JsonNode? node, out JsonNode? rewritten)
        {
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
                rewritten = RewriteArray(arr);
                return true;
            }

            throw new ArgumentOutOfRangeException(nameof(node));
        }

        private JsonObject RewriteArray(JsonArray value)
        {
            Dictionary<string, List<JsonNode?>> result = new();

            foreach (JsonNode? element in value)
            {
                switch (element)
                {
                    case JsonArray:
                        throw ThrowHelper.ExpectObjectButReceivedArray(element.GetPath());
                    case JsonValue:
                        throw ThrowHelper.ExpectObjectButReceivedScalar(element.GetPath());
                    case JsonObject obj:
                    {
                        if (obj.TryGetPropertyValue(Key, out JsonNode? jsonNode) &&
                            jsonNode.TryConvertToString(out string? convertedValue))
                        {
                            if (!result.ContainsKey(convertedValue))
                            {
                                result[convertedValue] = new List<JsonNode?>();
                            }

                            result[convertedValue].Add(jsonNode);
                        }

                        break;
                    }
                }
            }

            return result.ToJsonNode();
        }
    }
}
