using System.Text.Json.Nodes;

namespace HotChocolate.Lodash
{
    public class MapOperation : AggregationOperation
    {
        public MapOperation(string key)
        {
            Key = key;
        }

        public string Key { get; }

        public override JsonNode? Rewrite(JsonNode? node)
        {
            if (node is JsonObject obj)
            {
                if (obj.TryGetPropertyValue(Key, out var jsonNode))
                {
                    obj.Remove(Key);
                    return jsonNode;
                }

                return null;
            }
            else if (node is JsonArray arr)
            {
                for (var i = arr.Count - 1; i >= 0; i--)
                {
                    JsonNode? result = Rewrite(arr[i]);
                    arr.RemoveAt(i);
                    if (result is not null)
                    {
                        arr.Insert(i, result);
                    }
                }

                return arr;
            }

            return null;
        }
    }
}
