using System;

namespace StrawberryShake
{
    public interface IOperationRequestContext
    {
        OperationRequest Request { get; }

        IServiceProvider Services { get; }

        IExecutionResult? Result { get; set; }
    }
}
