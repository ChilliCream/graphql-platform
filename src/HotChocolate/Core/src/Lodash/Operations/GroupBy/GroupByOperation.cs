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

        public override JsonNode? Rewrite(JsonNode? node)
        {
            Dictionary<string, List<JsonNode?>> data = new();
            GroupByField(node, data);

            var jsonObject = new JsonObject();
            foreach (KeyValuePair<string, List<JsonNode?>> pair in data)
            {
                jsonObject[pair.Key] = new JsonArray(pair.Value.ToArray());
            }

            return jsonObject;
        }

        private void GroupByField(JsonNode? node, IDictionary<string, List<JsonNode?>> data)
        {
            if (node is JsonObject obj)
            {
                if (obj.TryGetPropertyValue(Key, out JsonNode? jsonNode))
                {
                    if (jsonNode is not null &&
                        jsonNode.GetValue<JsonElement>().TryConvertToString(out string? converted))
                    {
                        if (!data.ContainsKey(converted))
                        {
                            data[converted] = new List<JsonNode?>();
                        }

                        data[converted].Add(obj);
                    }
                    // throw?
                }
                // throw?
            }
            else if (node is JsonArray arr)
            {
                for (var i = arr.Count - 1; i >= 0; i--)
                {
                    GroupByField(arr[i], data);
                    arr.RemoveAt(i);
                }
            }
        }
    }
}
