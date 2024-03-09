using System;

namespace HotChocolate.Execution;

public class OperationRequestBuilderException : Exception
{
    public OperationRequestBuilderException() { }

    public OperationRequestBuilderException(string message)
        : base(message) { }

    public OperationRequestBuilderException(string message, Exception inner)
        : base(message, inner) { }
}
