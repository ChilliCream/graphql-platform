using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HotChocolate.AspNetCore
{
    internal class ClientQueryRequest
    {
        [JsonProperty("operationName")]
        public string OperationName { get; set; }

        [JsonProperty("namedQuery")]
        public string NamedQuery { get; set; }

        [JsonProperty("query")]
        public string Query { get; set; }

        [JsonProperty("variables")]
        public JObject Variables { get; set; }
    }
}
