using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Lodash
{
    public class LodashKeyByOperation : LodashOperation
    {
        public LodashKeyByOperation(string key)
        {
            Key = key;
        }

        public string Key { get; }

        public override JsonNode? Rewrite(JsonNode? node)
        {
            Dictionary<string, JsonNode?> data = new();
            KeyByField(node, data);

            var jsonObject = new JsonObject();
            foreach (KeyValuePair<string, JsonNode?> pair in data)
            {
                jsonObject[pair.Key] = pair.Value;
            }

            return jsonObject;
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

        public static readonly ILodashOperationFactory Factory =
            new LodashKeyByOperationFactory();

        private class LodashKeyByOperationFactory : ILodashOperationFactory
        {
            public bool TryCreateOperation(
                DirectiveNode directiveNode,
                [NotNullWhen(true)] out LodashOperation? operation)
            {
                ArgumentNode? map =
                    directiveNode.Arguments.FirstOrDefault(x => x.Name.Value == "keyBy");

                if (map is not null && map.Value is StringValueNode stringValueNode)
                {
                    operation = new LodashKeyByOperation(stringValueNode.Value);
                    return true;
                }

                operation = null;
                return false;
            }
        }
    }
}
