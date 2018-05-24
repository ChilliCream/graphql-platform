using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace HotChocolate.Execution
{
    public class QueryResult
    {
        public QueryResult(OrderedDictionary data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            Data = data;
        }

        public QueryResult(List<IQueryError> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }
            Errors = errors.ToImmutableList();
        }

        public QueryResult(OrderedDictionary data, List<IQueryError> errors)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            Data = data;
            Errors = errors.ToImmutableList();
        }

        public OrderedDictionary Data { get; }
        public ImmutableList<IQueryError> Errors { get; }
    }
}
