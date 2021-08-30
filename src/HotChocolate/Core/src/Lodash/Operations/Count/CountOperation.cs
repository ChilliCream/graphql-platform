using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace HotChocolate.Lodash
{
    public class CountOperation : AggregationOperation
    {
        public CountOperation(string? by)
        {
            By = by;
        }

        public string? By { get; }

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
                rewritten = By is null ? RewriteArray(arr) : RewriteArrayBy(arr, By);
                return true;
            }

            throw new ArgumentOutOfRangeException(nameof(node));
        }

        private JsonNode RewriteArray(JsonArray value) => value.Count;

        private JsonObject RewriteArrayBy(JsonArray value, string by)
        {
            Dictionary<string, int> result = new();

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
                        if (obj.TryGetPropertyValue(by, out JsonNode? jsonNode) &&
                            jsonNode.TryConvertToString(out string? convertedValue))
                        {
                            if (!result.ContainsKey(convertedValue))
                            {
                                result[convertedValue] = 0;
                            }

                            result[convertedValue]++;
                        }

                        break;
                    }
                }
            }

            return result.ToJsonNode();
        }
    }
}
