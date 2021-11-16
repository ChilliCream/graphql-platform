using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Lodash
{
    public class UniqueOperation : AggregationOperation
    {
        public UniqueOperation(string? by)
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

        private JsonArray RewriteArray(JsonArray value)
        {
            HashSet<object?> values = new();
            JsonArray result = new();

            while (value.Count > 0)
            {
                JsonNode? element = value[0];
                value.RemoveAt(0);
                switch (element)
                {
                    case JsonArray:
                        throw ThrowHelper.ExpectScalarButReceivedArray(element.GetPath());
                    case JsonObject:
                        throw ThrowHelper.ExpectScalarButReceivedObject(element.GetPath());
                    case JsonValue:
                        if (!element.TryConvertToComparable(out IComparable? convertedScalar) ||
                            values.Add(convertedScalar))
                        {
                            result.Add(element);
                        }
                        break;
                    case null when values.Add(element):
                        result.Add(element);
                        break;
                }
            }

            return result;
        }

        private JsonArray RewriteArrayBy(JsonArray value, string by)
        {
            HashSet<object?> values = new();
            JsonArray result = new();

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
                        if (!obj.TryGetPropertyValue(by, out JsonNode? jsonNode) ||
                            !jsonNode.TryConvertToComparable(out IComparable? convertedValue) ||
                            values.Add(convertedValue))
                        {
                            result.Add(element);
                        }
                        break;
                    case null when values.Add(element):
                        result.Add(element);
                        break;
                }
            }

            return result;
        }
    }
}
