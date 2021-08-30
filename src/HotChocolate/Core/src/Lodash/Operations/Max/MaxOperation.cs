using System;
using System.Text.Json.Nodes;

namespace HotChocolate.Lodash
{
    public class MaxOperation : AggregationOperation
    {
        public MaxOperation(string? by)
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

        private JsonNode? RewriteArray(JsonArray list)
        {
            if (list.Count == 0)
            {
                return null;
            }

            IComparable? lastValue = null;
            JsonNode? result = list[0];

            while (list.Count > 0)
            {
                JsonNode? element = list[0];
                list.RemoveAt(0);
                if (element is JsonValue node &&
                    node.TryConvertToComparable(out IComparable? converted) &&
                    (lastValue is null || lastValue.CompareTo(converted) < 0))
                {
                    lastValue = converted;
                    result = element;
                }
            }

            return result;
        }

        private JsonObject? RewriteArrayBy(JsonArray value, string by)
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
                        if (obj.TryGetPropertyValue(by, out JsonNode? jsonNode) &&
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
