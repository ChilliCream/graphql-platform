using System.Collections.Generic;
using Newtonsoft.Json;

namespace HotChocolate.AspNetCore.Utilities
{
    public class ClientQueryRequest
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("operationName")]
        public string OperationName { get; set; }

        [JsonProperty("query")]
        public string Query { get; set; }

        [JsonProperty("variables")]
        public Dictionary<string, object> Variables { get; set; }
    }
}
