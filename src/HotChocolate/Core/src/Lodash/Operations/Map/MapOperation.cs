using System.Text.Json;
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

        public override bool Rewrite(JsonNode? node, out JsonNode? rewritten)
        {
            if (node is JsonObject obj)
            {
                if (obj.TryGetPropertyValue(Key, out JsonNode? jsonNode))
                {
                    obj.Remove(Key);
                    rewritten = jsonNode;
                    return true;
                }

                rewritten = null;
                return false;
            }
            else if (node is JsonArray arr)
            {
                for (var i = arr.Count - 1; i >= 0; i--)
                {
                    JsonNode? element = arr[i];
                    arr.RemoveAt(i);
                    var result = Rewrite(element, out JsonNode? inner);
                    if (result)
                    {
                        arr.Insert(i, inner);
                    }
                }

                rewritten = arr;
                return true;
            }
            else if (node is JsonValue)
            {
                throw ThrowHelper.ExpectObjectButReceivedScalar(node.GetPath());
            }

            rewritten = null;
            return true;
        }
    }
}
