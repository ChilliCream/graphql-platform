using System.Collections.Generic;
using Prometheus.Execution;

namespace Prometheus.AspNet
{
    public class QueryResultDto
    {
        public Dictionary<string, object> Data { get; set; }
        public List<QueryError> Errors { get; set; }
    }
}