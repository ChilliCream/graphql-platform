using System.Collections.Generic;
using System.Collections.Immutable;

namespace HotChocolate.Execution
{
    public class QueryResult
    {
        public QueryResult(Dictionary<string, object> data)
        {
            if (data == null)
            {
                throw new System.ArgumentNullException(nameof(data));
            }
            Data = data.ToImmutableDictionary();
        }

        public QueryResult(List<IQueryError> errors)
        {
            if (errors == null)
            {
                throw new System.ArgumentNullException(nameof(errors));
            }
            Errors = errors.ToImmutableList();
        }

        public QueryResult(Dictionary<string, object> data, List<IQueryError> errors)
        {
            if (data == null)
            {
                throw new System.ArgumentNullException(nameof(data));
            }

            if (errors == null)
            {
                throw new System.ArgumentNullException(nameof(errors));
            }

            Data = data.ToImmutableDictionary();
            Errors = errors.ToImmutableList();
        }

        public ImmutableDictionary<string, object> Data { get; }
        public ImmutableList<IQueryError> Errors { get; }
    }
}
