using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace HotChocolate.Lodash
{
    internal static class DictionaryJsonExtensions
    {
        public static JsonObject ToJsonNode(this IDictionary<string, int> dictionary)
        {
            JsonObject result = new();
            foreach (KeyValuePair<string, int> pair in dictionary)
            {
                result[pair.Key] = pair.Value;
            }

            return result;
        }
    }
}
