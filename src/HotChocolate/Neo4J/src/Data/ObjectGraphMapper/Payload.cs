#nullable enable
using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J
{
    public class Error
    {
        public Error(string code, string message)
        {
            Code = code;
            Message = message;
        }
        public string Code { get; set; }
        public string Message { get; set; }
    }

    public abstract class Payload<T>
    {
        protected Payload(List<T>? returning = null, IReadOnlyList<Error>? errors = null)
        {

            Returning = returning;
            Errors = errors;
        }
        public IReadOnlyList<Error>? Errors { get; }

        public List<T>? Returning { get; set; }
    }
}
