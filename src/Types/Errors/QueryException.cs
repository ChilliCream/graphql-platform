using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HotChocolate.Errors
{
    [Serializable]
    public class QueryException
        : Exception
    {
        public QueryException(string message)
            : base(message)
        {
            var errors = new List<IQueryError> { new QueryError(message) };
            Errors = errors.AsReadOnly();
        }

        public QueryException(IQueryError error)
        {
            if (error == null)
            {
                Errors = Array.Empty<IQueryError>();
            }
            else
            {
                var errors = new List<IQueryError> { error };
                Errors = errors.AsReadOnly();
            }
        }

        public QueryException(params IQueryError[] errors)
        {
            Errors = new List<IQueryError>(
                errors ?? Array.Empty<IQueryError>())
                    .AsReadOnly();
        }

        public QueryException(IEnumerable<IQueryError> errors)
        {
            Errors = new List<IQueryError>(
               errors ?? Array.Empty<IQueryError>())
                   .AsReadOnly();
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
