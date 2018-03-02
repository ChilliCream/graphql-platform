using System.Collections.Generic;
using Zeus.Execution;

namespace Zeus.AspNet
{
    public class QueryResultDto
    {
        public Dictionary<string, object> Data { get; set; }
        public List<QueryError> Errors { get; set; }
    }
}