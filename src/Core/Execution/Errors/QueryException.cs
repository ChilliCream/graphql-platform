using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.Serialization;

namespace HotChocolate.Execution
{
    [Serializable]
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
            Errors = errors?.ToImmutableList()
                ?? ImmutableList<IQueryError>.Empty;
        }

        protected QueryException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }

        public IReadOnlyCollection<IQueryError> Errors { get; }
    }
}
