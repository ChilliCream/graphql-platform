using System.Collections.Generic;
using HotChocolate.Execution;

namespace HotChocolate.AspNetClassic
{
    public class ClientQueryResult
    {
        public Dictionary<string, object> Data { get; set; }
        public List<QueryError> Errors { get; set; }
    }
}
