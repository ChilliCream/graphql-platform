using System.Text.Json;
using System.Text.Json.Nodes;

namespace HotChocolate.Lodash
{
    public class MeanByOperation : AggregationOperation
    {
        public MeanByOperation(string key)
        {
            Key = key;
        }

        public string Key { get; }

        public override JsonNode? Rewrite(JsonNode? node)
        {
            if (node is null)
            {
                return null;
            }

            double result = 0;
            double count = 0;

            void SumByField(JsonNode? node)
            {
                if (node is JsonObject obj)
                {
                    if (obj.TryGetPropertyValue(Key, out JsonNode? jsonNode))
                    {
                        //TODO : String also?
                        if (jsonNode is not null &&
                            jsonNode.GetValue<JsonElement>()
                                .TryConvertToNumber(out var number))
                        {
                            result += number;
                            count++;
                        }
                        // throw?
                    }
                    // throw?
                }
                else if (node is JsonArray arr)
                {
                    for (var i = arr.Count - 1; i >= 0; i--)
                    {
                        SumByField(arr[i]);
                        arr.RemoveAt(i);
                    }
                }
            }

            SumByField(node);

            return result / count;
        }
    }
}
