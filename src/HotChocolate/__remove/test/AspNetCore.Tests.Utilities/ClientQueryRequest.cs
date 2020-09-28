using System.Collections.Generic;
using Newtonsoft.Json;

namespace HotChocolate.AspNetCore.Tests.Utilities
{
    public class ClientQueryRequest
    {
        [JsonProperty("operationName")]
        public string OperationName { get; set; }

        [JsonProperty("namedQuery")]
        public string NamedQuery { get; set; }

        [JsonProperty("query")]
        public string Query { get; set; }

        [JsonProperty("variables")]
        public Dictionary<string, object> Variables { get; set; }
    }
}
