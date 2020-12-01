using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace StrawberryShake
{
    [Serializable]
    public class GraphQLException
        : Exception
    {
        public GraphQLException(IError error)
            : base(CreateErrorMessage(error))
        {
            Errors = error == null
                ? Array.Empty<IError>()
                : new[] { error };
        }

        public GraphQLException(IReadOnlyList<IError> errors)
            : base(CreateErrorMessage(errors))
        {
            Errors = errors;
        }

        public GraphQLException()
        {
            Errors = Array.Empty<IError>();
        }

        public GraphQLException(string message)
            : base(message)
        {
            Errors = Array.Empty<IError>();
        }

        public GraphQLException(string message, Exception inner)
            : base(message, inner)
        {
            Errors = Array.Empty<IError>();
        }

        protected GraphQLException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
            Errors = Array.Empty<IError>();
        }

        public IReadOnlyList<IError> Errors { get; }

        private static string CreateErrorMessage(IReadOnlyList<IError> errors)
        {
            if (errors is null || errors.Count == 0)
            {
                // TODO : resources
                return "Unexpected GraphQL exception occurred.";
            }

            if (errors.Count == 1)
            {
                return errors[0].Message;
            }

            var message = new StringBuilder();

            // TODO : resources
            message.AppendLine("Multiple GraphQL errors occurred:");
            foreach (IError error in errors)
            {
                message.AppendLine(error.Message);
            }

            return message.ToString();
        }

        private static string CreateErrorMessage(IError error)
        {
            if (error is null)
            {
                // TODO : resources
                return "Unexpected GraphQL exception occurred.";
            }
            return error.Message;
        }
    }
}
