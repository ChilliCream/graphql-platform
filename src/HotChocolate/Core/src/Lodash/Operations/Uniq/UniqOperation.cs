using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HotChocolate.Lodash
{
    public class UniqOperation : AggregationOperation
    {
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
                RewriteArray(out rewritten, arr);
                return true;
            }

            throw new ArgumentOutOfRangeException(nameof(node));
        }

        private static void RewriteArray(out JsonNode? rewritten, JsonArray arr)
        {
            HashSet<object> values = new();
            JsonArray result = new();
            bool isNullProcessed = false;
            while (arr.Count > 0)
            {
                JsonNode? element = arr[0];
                arr.RemoveAt(0);

                if (element is null)
                {
                    if (!isNullProcessed)
                    {
                        isNullProcessed = true;
                        result.Add(null);
                    }

                    continue;
                }

                if (element is JsonValue &&
                    element.GetValue<JsonElement>()
                        .TryConvertToComparable(out IComparable? comparable))
                {
                    if (values.Add(comparable))
                    {
                        result.Add(element);
                    }
                }
                else
                {
                    result.Add(element);
                }
            }

            rewritten = result;
        }
    }
}
