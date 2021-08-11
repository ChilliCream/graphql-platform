using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace HotChocolate.Lodash
{
    public class AggregationRewriterStep
    {
        public AggregationRewriterStep(
            string fieldName,
            IReadOnlyList<AggregationOperation> operations,
            IReadOnlyList<AggregationRewriterStep> next)
        {
            FieldName = fieldName;
            Operations = operations;
            Next = next;
        }

        public string FieldName { get; }

        public IReadOnlyList<AggregationOperation> Operations { get; }

        public IReadOnlyList<AggregationRewriterStep> Next { get; }

        public JsonNode? Rewrite(JsonNode? node)
        {
            if (node is JsonObject obj)
            {
                if (obj.TryGetPropertyValue(FieldName, out JsonNode? jsonNode))
                {
                    for (var i = 0; i < Next.Count && jsonNode is not null; i++)
                    {
                        jsonNode = Next[i].Rewrite(jsonNode);
                    }

                    for (var i = 0; i < Operations.Count && jsonNode is not null; i++)
                    {
                        jsonNode = Operations[i].Rewrite(jsonNode);
                    }

                    if (jsonNode is not null)
                    {
                        obj[FieldName] = jsonNode;
                    }
                    else
                    {
                        obj.Remove(FieldName);
                    }
                }
            }
            else if (node is JsonArray arr)
            {
                for (var index = arr.Count - 1; index >= 0; index--)
                {
                    JsonNode? item = arr[index];
                    if (Rewrite(item) is { } newNode)
                    {
                        arr.RemoveAt(index);
                        arr.Insert(index, newNode);
                    }
                }
            }

            return node;
        }
    }
}
