using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace HotChocolate.AspNetCore
{
    internal class QueryRequest
    {
        public string OperationName { get; set; }
        public string NamedQuery { get; set; }
        public string Query { get; set; }
        public Dictionary<string, JToken> Variables { get; set; }
    }
}
