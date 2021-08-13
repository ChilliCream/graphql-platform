using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HotChocolate.Lodash
{
    public class CountByOperation : AggregationOperation
    {
        public CountByOperation(string key)
        {
            Key = key;
        }

        public string Key { get; }

        public override bool Rewrite(JsonNode? node, out JsonNode? rewritten)
        {
            Dictionary<string, int> data = new();
            CountByField(node, data);

            var jsonObject = new JsonObject();
            foreach (KeyValuePair<string, int> pair in data)
            {
                jsonObject[pair.Key] = pair.Value;
            }

            rewritten = jsonObject;
            return true;
        }

        private void CountByField(JsonNode? node, IDictionary<string, int> data)
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
                            data[converted] = 0;
                        }

                        data[converted]++;
                    }
                    // throw?
                }
                // throw?
            }
            else if (node is JsonArray arr)
            {
                for (var i = arr.Count - 1; i >= 0; i--)
                {
                    CountByField(arr[i], data);
                }
            }
        }

    }
}
