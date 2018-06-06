using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace HotChocolate.Execution
{
    public class QueryException
        : Exception
    {
        public QueryException(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            Errors = ImmutableList<IQueryError>.Empty.Add(new QueryError(message));
        }


        public QueryException(IQueryError error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            Errors = ImmutableList<IQueryError>.Empty.Add(error);
        }

        public QueryException(params IQueryError[] errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            Errors = errors.ToImmutableList(); ;
        }

        public QueryException(IEnumerable<IQueryError> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            Errors = errors.ToImmutableList();
        }

        public IReadOnlyCollection<IQueryError> Errors { get; }
    }
}
