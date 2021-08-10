using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Lodash
{
    public class LodashKeysOperation : LodashOperation
    {
        public override JsonNode? Rewrite(JsonNode? node)
        {
            if (node is null)
            {
                return null;
            }

            if (node is JsonArray)
            {
                throw ThrowHelper.ExpectObjectButReceivedArray(node.GetPath());
            }

            if (node is JsonObject obj)
            {
                JsonArray result = new();

                foreach (KeyValuePair<string, JsonNode?> pair in obj)
                {
                    result.Add(pair.Key);
                }

                return result;
            }

            throw ThrowHelper.ExpectObjectButReceivedScalar(node.GetPath());
        }

        public static readonly ILodashOperationFactory Factory =
            new LodashKeysOperationFactory();

        private class LodashKeysOperationFactory : ILodashOperationFactory
        {
            public bool TryCreateOperation(
                DirectiveNode directiveNode,
                [NotNullWhen(true)] out LodashOperation? operation)
            {
                ArgumentNode? map =
                    directiveNode.Arguments.FirstOrDefault(x => x.Name.Value == "keys");

                if (map is not null && map.Value is EnumValueNode {Value: "none"}) {
                    operation = new LodashKeysOperation();
                    return true;
                }

                operation = null;
                return false;
            }
        }
    }
}
