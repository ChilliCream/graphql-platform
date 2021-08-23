using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Lodash
{
    public class UniqByOperation : AggregationOperation
    {
        public UniqByOperation(string key)
        {
            Key = key;
        }

        public string Key { get; }

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
                rewritten = RewriteArray(arr);
                return true;
            }

            throw new ArgumentOutOfRangeException(nameof(node));
        }

        private JsonArray RewriteArray(JsonArray value)
        {
            HashSet<object> values = new();
            JsonArray result = new();
            bool isNullProcessed = false;

            while (value.Count > 0)
            {
                JsonNode? element = value[0];
                value.RemoveAt(0);
                switch (element)
                {
                    case JsonArray:
                        throw ThrowHelper.ExpectObjectButReceivedArray(element.GetPath());
                    case JsonValue:
                        throw ThrowHelper.ExpectObjectButReceivedScalar(element.GetPath());
                    case null:
                    {
                        if (!isNullProcessed)
                        {
                            isNullProcessed = true;
                            result.Add(null);
                        }

                        break;
                    }
                    case JsonObject obj:
                    {
                        if (!obj.TryGetPropertyValue(Key, out JsonNode? jsonNode) ||
                            !jsonNode.TryConvertToComparable(out IComparable? convertedValue) ||
                            values.Add(convertedValue))
                        {
                            result.Add(obj);
                        }

                        break;
                    }
                }
            }

            return result;
        }
    }
}
