using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Lodash
{
    public class LodashCountByOperation : LodashOperation
    {
        public LodashCountByOperation(string key)
        {
            Key = key;
        }

        public string Key { get; }

        public override JsonNode? Rewrite(JsonNode? node)
        {
            Dictionary<string, int> data = new();
            CountByField(node, data);

            var jsonObject = new JsonObject();
            foreach (KeyValuePair<string, int> pair in data)
            {
                jsonObject[pair.Key] = pair.Value;
            }

            return jsonObject;
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

        public static readonly ILodashOperationFactory Factory =
            new LodashCountByOperationFactory();

        private class LodashCountByOperationFactory : ILodashOperationFactory
        {
            public bool TryCreateOperation(
                DirectiveNode directiveNode,
                [NotNullWhen(true)] out LodashOperation? operation)
            {
                ArgumentNode? map =
                    directiveNode.Arguments.FirstOrDefault(x => x.Name.Value == "countBy");

                if (map is not null && map.Value is StringValueNode stringValueNode)
                {
                    operation = new LodashCountByOperation(stringValueNode.Value);
                    return true;
                }

                operation = null;
                return false;
            }
        }
    }
}
