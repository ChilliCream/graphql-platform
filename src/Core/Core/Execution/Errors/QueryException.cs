using System;
using System.Collections.Generic;
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
            var errors = new List<IError> { new QueryError(message) };
            Errors = errors.AsReadOnly();
        }

        public QueryException(IError error)
        {
            if (error == null)
            {
                Errors = Array.Empty<IError>();
            }
            else
            {
                var errors = new List<IError> { error };
                Errors = errors.AsReadOnly();
            }
        }

        public QueryException(params IError[] errors)
        {
            Errors = new List<IError>(
                errors ?? Array.Empty<IError>())
                    .AsReadOnly();
        }

        public QueryException(IEnumerable<IError> errors)
        {
            Errors = new List<IError>(
               errors ?? Array.Empty<IError>())
                   .AsReadOnly();
        }

        protected QueryException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }

        public IReadOnlyCollection<IError> Errors { get; }
    }
}
