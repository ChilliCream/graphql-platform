using System.Text.Json.Nodes;

namespace HotChocolate.Lodash
{
    public abstract class AggregationOperation
    {
        public abstract JsonNode? Rewrite(JsonNode? node);
    }
}
