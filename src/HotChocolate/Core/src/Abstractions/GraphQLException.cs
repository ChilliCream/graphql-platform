using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HotChocolate
{
    [Serializable]
    public class GraphQLException
        : Exception
    {
        public GraphQLException(string message)
            : this(ErrorBuilder.New().SetMessage(message).Build())
        {
        }

        public GraphQLException(IError error)
            : base(error?.Message)
        {
            Errors = error is null
                ? Array.Empty<IError>()
                : new[] { error };
        }

        public GraphQLException(params IError[] errors)
        {
            Errors = errors ?? Array.Empty<IError>();
        }

        public GraphQLException(IEnumerable<IError> errors)
        {
            Errors = new List<IError>(
               errors ?? Array.Empty<IError>())
                   .AsReadOnly();
        }

        protected GraphQLException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }

        public IReadOnlyList<IError> Errors { get; }
    }
}
