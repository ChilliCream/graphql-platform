using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

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

            HashSet<object> values = new();
            JsonArray array = new();

            void UniqByField(JsonNode? node)
            {
                if (node is JsonObject obj)
                {
                    if (obj.TryGetPropertyValue(Key, out JsonNode? jsonNode))
                    {
                        if (jsonNode is JsonValue &&
                            jsonNode.GetValue<JsonElement>()
                                .TryConvertToComparable(out IComparable? converted))
                        {
                            if (values.Add(converted))
                            {
                                array.Add(obj);
                            }
                        }
                        else
                        {
                            array.Add(obj);
                        }
                        // throw?
                    }
                    // throw?
                }
                else if (node is JsonArray arr)
                {
                    while (arr.Count > 0)
                    {
                        JsonNode? elm = arr[0];
                        arr.RemoveAt(0);
                        UniqByField(elm);
                    }
                }
            }

            UniqByField(node);

            rewritten = array;
            return true;
        }
    }
}
