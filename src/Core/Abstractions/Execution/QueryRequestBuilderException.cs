using System;

namespace HotChocolate.Execution
{
    [Serializable]
    public class QueryRequestBuilderException
        : Exception
    {
        public QueryRequestBuilderException() { }
        public QueryRequestBuilderException(string message)
            : base(message) { }
        public QueryRequestBuilderException(string message, Exception inner)
            : base(message, inner) { }
        protected QueryRequestBuilderException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
