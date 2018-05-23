using System.Collections.Generic;
using HotChocolate.Execution;

namespace HotChocolate.AspNetCore
{
    public class QueryResultDto
    {
        public Dictionary<string, object> Data { get; set; }
        public List<QueryError> Errors { get; set; }
    }
}
