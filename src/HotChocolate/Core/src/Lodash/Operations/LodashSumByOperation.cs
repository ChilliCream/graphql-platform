using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Lodash
{
    public class LodashSumByOperation : LodashOperation
    {
        public LodashSumByOperation(string key)
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

            return result;
        }

        public static readonly ILodashOperationFactory Factory =
            new LodashSumByOperationFactory();

        private class LodashSumByOperationFactory : ILodashOperationFactory
        {
            public bool TryCreateOperation(
                DirectiveNode directiveNode,
                [NotNullWhen(true)] out LodashOperation? operation)
            {
                ArgumentNode? map =
                    directiveNode.Arguments.FirstOrDefault(x => x.Name.Value == "sumBy");

                if (map is not null && map.Value is StringValueNode stringValueNode)
                {
                    operation = new LodashSumByOperation(stringValueNode.Value);
                    return true;
                }

                operation = null;
                return false;
            }
        }
    }
}
