using System.Text.Json.Nodes;

namespace HotChocolate.Lodash
{
    public abstract class LodashOperation
    {
        public abstract JsonNode? Rewrite(JsonNode? node);
    }
}
