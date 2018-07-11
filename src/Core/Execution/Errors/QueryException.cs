using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace HotChocolate.Execution
{
    public class QueryException
        : Exception
    {
        public QueryException(string message)
            : base(message)
        {
            Errors = ImmutableList<IQueryError>.Empty
                .Add(new QueryError(message));
        }


        public QueryException(IQueryError error)
        {
            if (error == null)
            {
                Errors = ImmutableList<IQueryError>.Empty;
            }
            else
            {
                Errors = ImmutableList<IQueryError>.Empty.Add(error);
            }
        }

        public QueryException(params IQueryError[] errors)
        {
            Errors = errors?.ToImmutableList()
                ?? ImmutableList<IQueryError>.Empty;
        }

        public QueryException(IEnumerable<IQueryError> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            Errors = errors.ToImmutableList()
                ?? ImmutableList<IQueryError>.Empty;
        }

        public IReadOnlyCollection<IQueryError> Errors { get; }
    }
}
