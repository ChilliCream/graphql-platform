using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Lodash
{
    public class LodashUniqByOperation : LodashOperation
    {
        public LodashUniqByOperation(string key)
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

            return array;
        }

        public static readonly ILodashOperationFactory Factory =
            new LodashUniqByOperationFactory();

        private class LodashUniqByOperationFactory : ILodashOperationFactory
        {
            public bool TryCreateOperation(
                DirectiveNode directiveNode,
                [NotNullWhen(true)] out LodashOperation? operation)
            {
                ArgumentNode? map =
                    directiveNode.Arguments.FirstOrDefault(x => x.Name.Value == "uniqBy");

                if (map is not null && map.Value is StringValueNode stringValueNode)
                {
                    operation = new LodashUniqByOperation(stringValueNode.Value);
                    return true;
                }

                operation = null;
                return false;
            }
        }
    }
}
