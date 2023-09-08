using System;

namespace HotChocolate.Execution;

[Serializable]
public class QueryRequestBuilderException : Exception
{
    public QueryRequestBuilderException() { }

    public QueryRequestBuilderException(string message)
        : base(message) { }

    public QueryRequestBuilderException(string message, Exception inner)
        : base(message, inner) { }

#if NET8_0_OR_GREATER
    [Obsolete(
        "This API supports obsolete formatter-based serialization. " +
        "It should not be called or extended by application code.",
        true)]
#endif
    protected QueryRequestBuilderException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context)
        : base(info, context) { }
}
