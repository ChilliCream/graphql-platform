
using System;

namespace Prometheus.Execution
{
    public class QueryError
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