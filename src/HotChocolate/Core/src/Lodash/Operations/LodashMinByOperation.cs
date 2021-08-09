using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Lodash
{
    public class LodashMinByOperation : LodashOperation
    {
        public LodashMinByOperation(string key)
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

            IComparable? result = null;
            JsonNode? resultNode = null;

            void MinByField(JsonNode? node)
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
                                if (result.CompareTo(converted) > 0)
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
                        MinByField(arr[i]);
                        arr.RemoveAt(i);
                    }
                }
            }

            MinByField(node);

            return resultNode;
        }

        public static readonly ILodashOperationFactory Factory =
            new LodashMinByOperationFactory();

        private class LodashMinByOperationFactory : ILodashOperationFactory
        {
            public bool TryCreateOperation(
                DirectiveNode directiveNode,
                [NotNullWhen(true)] out LodashOperation? operation)
            {
                ArgumentNode? map =
                    directiveNode.Arguments.FirstOrDefault(x => x.Name.Value == "minBy");

                if (map is not null && map.Value is StringValueNode stringValueNode)
                {
                    operation = new LodashMinByOperation(stringValueNode.Value);
                    return true;
                }

                operation = null;
                return false;
            }
        }
    }
}
