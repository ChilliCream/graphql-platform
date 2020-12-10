using System;

namespace StrawberryShake
{
    public interface IOperationRequestContext
    {
        IOperationRequest Request { get; }

        IServiceProvider Services { get; }

        IExecutionResult? Result { get; set; }
    }
}
