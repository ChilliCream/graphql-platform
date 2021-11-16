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

        public bool Rewrite(JsonNode? node, out JsonNode? rewritten)
        {
            if (node is JsonObject obj)
            {
                if (obj.TryGetPropertyValue(FieldName, out JsonNode? jsonNode))
                {
                    obj.Remove(FieldName);
                    var omitProperty = false;
                    for (var i = 0;
                        i < Next.Count && jsonNode is not null && !omitProperty;
                        i++)
                    {
                        if (Next[i].Rewrite(jsonNode, out JsonNode? newNode))
                        {
                            jsonNode = newNode;
                        }
                        else
                        {
                            omitProperty = true;
                        }
                    }

                    for (var i = 0;
                        i < Operations.Count && jsonNode is not null && !omitProperty;
                        i++)
                    {
                        if (Operations[i].Rewrite(jsonNode, out JsonNode? newNode))
                        {
                            jsonNode = newNode;
                        }
                        else
                        {
                            omitProperty = true;
                        }
                    }

                    if (!omitProperty)
                    {
                        obj[FieldName] = jsonNode;
                    }
                }
            }
            else if (node is JsonArray arr)
            {
                for (var index = arr.Count - 1; index >= 0; index--)
                {
                    JsonNode? item = arr[index];
                    arr.RemoveAt(index);
                    if (Rewrite(item, out JsonNode? newNode))
                    {
                        arr.Insert(index, newNode);
                    }
                }
            }

            rewritten = node;
            return true;
        }
    }
}
