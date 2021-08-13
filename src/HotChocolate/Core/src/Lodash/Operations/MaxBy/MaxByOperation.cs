using System;
using System.Text.Json;
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

            IComparable? result = null;
            JsonNode? resultNode = null;

            void MaxByField(JsonNode? node)
            {
                if (node is JsonObject obj)
                {
                    if (obj.TryGetPropertyValue(Key, out JsonNode? jsonNode))
                    {
                        if (jsonNode is not null &&
                            jsonNode.GetValue<JsonElement>()
                                .TryConvertToComparable(out IComparable? converted))
                        {
                            if (result is null)
                            {
                                result = converted;
                                resultNode = obj;
                            }
                            else
                            {
                                if (result.CompareTo(converted) < 0)
                                {
                                    resultNode = obj;
                                }
                            }
                        }
                        // throw?
                    }
                    // throw?
                }
                else if (node is JsonArray arr)
                {
                    for (var i = arr.Count - 1; i >= 0; i--)
                    {
                        MaxByField(arr[i]);
                        arr.RemoveAt(i);
                    }
                }
            }

            MaxByField(node);


            rewritten = resultNode;
            return true;
        }
    }
}
