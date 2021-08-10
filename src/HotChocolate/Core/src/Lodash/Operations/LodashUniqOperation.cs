using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Lodash
{
    public class LodashUniqOperation : LodashOperation
    {
        public override JsonNode? Rewrite(JsonNode? node)
        {
            if (node is null)
            {
                return null;
            }

            if (node is JsonArray arr)
            {
                HashSet<object> values = new();
                JsonArray array = new();
                while (arr.Count > 0)
                {
                    JsonNode? element = arr[0];

                    if (element is null)
                    {
                        continue;
                    }

                    arr.RemoveAt(0);

                    if (element is JsonValue &&
                        element.GetValue<JsonElement>()
                        .TryConvertToComparable(out IComparable? comparable))
                    {
                        if (values.Add(comparable))
                        {
                            array.Add(element);
                        }
                    }
                    else
                    {
                        array.Add(element);
                    }
                }

                return array;
            }

            throw ThrowHelper.ExpectArrayButReceivedObject(node.GetPath());
        }

        public static readonly ILodashOperationFactory Factory =
            new LodashUniqOperationFactory();

        private class LodashUniqOperationFactory : ILodashOperationFactory
        {
            public bool TryCreateOperation(
                DirectiveNode directiveNode,
                [NotNullWhen(true)] out LodashOperation? operation)
            {
                ArgumentNode? map =
                    directiveNode.Arguments.FirstOrDefault(x => x.Name.Value == "uniq");

                if (map is not null && map.Value is EnumValueNode { Value: "none" })
                {
                    operation = new LodashUniqOperation();
                    return true;
                }

                operation = null;
                return false;
            }
        }
    }
}
