using Newtonsoft.Json.Linq;

namespace HotChocolate.AspNetCore
{
    internal class QueryRequestDto
    {
        public string OperationName { get; set; }
        public string NamedQuery { get; set; }
        public string Query { get; set; }
        public JObject Variables { get; set; }
    }
}
