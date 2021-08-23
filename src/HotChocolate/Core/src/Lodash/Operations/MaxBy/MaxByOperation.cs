using System;
using System.Text.Json.Nodes;

namespace HotChocolate.Lodash
{
    public class MaxByOperation : AggregationOperation
    {
        public MaxByOperation(string key)
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

        private JsonObject? RewriteArray(JsonArray value)
        {
            if (value.Count == 0)
            {
                return null;
            }

            IComparable? lastValue = null;
            JsonObject? result = value[0] is JsonObject o ? o : null;

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
                    case JsonObject obj:
                    {
                        if (obj.TryGetPropertyValue(Key, out JsonNode? jsonNode) &&
                            jsonNode.TryConvertToComparable(out IComparable? converted))
                        {
                            if (result is null ||
                                lastValue is null ||
                                lastValue.CompareTo(converted) < 0)
                            {
                                lastValue = converted;
                                result = obj;
                            }
                        }

                        break;
                    }
                }
            }

            return result;
        }
    }
}
