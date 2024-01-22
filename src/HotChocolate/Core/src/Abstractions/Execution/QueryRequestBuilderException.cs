using System;

namespace HotChocolate.Execution;

public class QueryRequestBuilderException : Exception
{
    public QueryRequestBuilderException() { }

    public QueryRequestBuilderException(string message)
        : base(message) { }

    public QueryRequestBuilderException(string message, Exception inner)
        : base(message, inner) { }
}
