using System.Text.Json.Nodes;

namespace HotChocolate.Lodash
{
    public abstract class AggregationOperation
    {
        public abstract bool Rewrite(JsonNode? node, out JsonNode? rewritten);
    }
}
