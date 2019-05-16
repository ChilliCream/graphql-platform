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
            : this(ErrorBuilder.New().SetMessage(message).Build())
        {
        }

        public QueryException(IError error)
            : base(error?.Message)
        {
            Errors = error == null
                ? Array.Empty<IError>()
                : new[] { error };
        }

        public QueryException(params IError[] errors)
        {
            Errors = errors ?? Array.Empty<IError>();
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
