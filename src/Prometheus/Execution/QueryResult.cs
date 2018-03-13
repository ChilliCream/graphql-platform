using System.Collections.Generic;

namespace Prometheus.Execution
{
    public class QueryResult
    {
        public QueryResult(IReadOnlyDictionary<string, object> data)
        {
            Data = data;
        }

        public QueryResult(QueryError error)
        {
            Errors = new[] { error };
        }

        public QueryResult(IReadOnlyCollection<QueryError> errors)
        {
            Errors = errors;
        }

        public QueryResult(IReadOnlyDictionary<string, object> data, IReadOnlyCollection<QueryError> errors)
        {
            Data = data;
            Errors = errors;
        }

        public IReadOnlyDictionary<string, object> Data { get; }
        public IReadOnlyCollection<QueryError> Errors { get; }
    }
}