using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HotChocolate.Lodash
{
    public class KeyByOperation : AggregationOperation
    {
        public KeyByOperation(string key)
        {
            Key = key;
        }

        public string Key { get; }

        public override bool Rewrite(JsonNode? node, out JsonNode? rewritten)
        {
            Dictionary<string, JsonNode?> data = new();
            KeyByField(node, data);

            var jsonObject = new JsonObject();
            foreach (KeyValuePair<string, JsonNode?> pair in data)
            {
                jsonObject[pair.Key] = pair.Value;
            }

            rewritten = jsonObject;
            return true;
        }

        private void KeyByField(JsonNode? node, IDictionary<string, JsonNode?> data)
        {
            if (node is JsonObject obj)
            {
                if (obj.TryGetPropertyValue(Key, out JsonNode? jsonNode))
                {
                    if (jsonNode is not null &&
                        jsonNode.GetValue<JsonElement>().TryConvertToString(out string? converted))
                    {
                        data[converted] = node;
                    }
                    // throw?
                }
                // throw?
            }
            else if (node is JsonArray arr)
            {
                for (var i = arr.Count - 1; i >= 0; i--)
                {
                    KeyByField(arr[i], data);
                    arr.RemoveAt(i);
                }
            }
        }
    }
}
