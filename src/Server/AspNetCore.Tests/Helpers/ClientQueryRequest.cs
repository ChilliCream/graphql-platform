using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HotChocolate.AspNetCore
{
    internal class ClientQueryRequest
    {
        public string OperationName { get; set; }
        public string NamedQuery { get; set; }
        public string Query { get; set; }
        public JObject Variables { get; set; }

        [JsonIgnore]
        public Dictionary<string, ICollection<ClientQueryRequestFile>>
            Files { get; set; } // dictionary key is variable name
    }
}
