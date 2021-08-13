using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HotChocolate.Lodash
{
    public class UniqOperation : AggregationOperation
    {
        public override bool Rewrite(JsonNode? node, out JsonNode? rewritten)
        {
            if (node is null)
            {
                rewritten = null;
                return false;
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

                rewritten = array;
                return true;
            }

            throw ThrowHelper.ExpectArrayButReceivedObject(node.GetPath());
        }
    }
}
