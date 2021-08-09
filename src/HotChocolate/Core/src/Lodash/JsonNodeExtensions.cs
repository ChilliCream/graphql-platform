using System.Text.Json.Nodes;

namespace HotChocolate.Lodash
{
    public static class JsonNodeExtensions
    {
        public static bool TryDetatchProperty(
            this JsonObject node,
            string propertyName,
            out JsonNode? jsonNode)
        {
            if (node.TryGetPropertyValue(propertyName, out jsonNode))
            {
                node.Remove(propertyName);

                return true;
            }

            return false;
        }
    }
}
