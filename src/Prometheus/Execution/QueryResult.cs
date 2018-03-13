using System.Collections.Generic;

namespace Prometheus.Execution
{
    public class QueryResult
    {
        public QueryResult(IReadOnlyDictionary<string, object> data)
        {
            Data = data;
        }

        public QueryResult(IQueryError error)
        {
            Errors = new[] { error };
        }

        public QueryResult(IReadOnlyCollection<IQueryError> errors)
        {
            Errors = errors;
        }

        public QueryResult(IReadOnlyDictionary<string, object> data, 
            IReadOnlyCollection<IQueryError> errors)
        {
            Data = data;
            Errors = errors;
        }

        public IReadOnlyDictionary<string, object> Data { get; }
        public IReadOnlyCollection<IQueryError> Errors { get; }
    }
}