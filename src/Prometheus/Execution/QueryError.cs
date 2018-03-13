
using System;

namespace Prometheus.Execution
{
    public interface IQueryError
    {
        string Message { get; }
    }

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