using System;
using Newtonsoft.Json;

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

        [JsonProperty("message", Order = 0)]
        public string Message { get; }
    }
}
