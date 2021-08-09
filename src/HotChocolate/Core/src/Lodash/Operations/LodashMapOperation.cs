using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Lodash
{
    public class LodashMapOperation : LodashOperation
    {
        public LodashMapOperation(string key)
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

        public static readonly ILodashOperationFactory Factory = new LodashMapOperationFactory();

        private class LodashMapOperationFactory : ILodashOperationFactory
        {
            public bool TryCreateOperation(
                DirectiveNode directiveNode,
                [NotNullWhen(true)] out LodashOperation? operation)
            {
                ArgumentNode? map =
                    directiveNode.Arguments.FirstOrDefault(x => x.Name.Value == "map");

                if (map is not null && map.Value is StringValueNode stringValueNode)
                {
                    operation = new LodashMapOperation(stringValueNode.Value);
                    return true;
                }

                operation = null;
                return false;
            }
        }
    }
}
