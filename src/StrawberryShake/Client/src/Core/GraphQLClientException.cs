using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace StrawberryShake
{
    [Serializable]
    public class GraphQLClientException : Exception
    {
        public GraphQLClientException(string message)
            : this(new ClientError(message))
        {
        }

        public GraphQLClientException(IClientError error)
            : base(error?.Message)
        {
            if (error is null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            Errors = new[] { error };
        }

        public GraphQLClientException(params IClientError[] errors)
        {
            Errors = errors;
        }

        public GraphQLClientException(IEnumerable<IClientError> errors)
        {
            Errors = new List<IClientError>(errors);
        }

        protected GraphQLClientException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
            Errors ??= Array.Empty<IClientError>();
        }

        public IReadOnlyList<IClientError> Errors { get; }
    }
}
