using System;

namespace HotChocolate.Execution
{
    public class QueryError
       : IQueryError
    {
        public QueryError(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException(
                    "The error message mustn't be null or empty.",
                    nameof(message));
            }

            Message = message;
        }

        public string Message { get; }
    }
}
