using System;
using StrawberryShake.Impl;

namespace StrawberryShake
{
    public interface IOperationRequestContext
    {
        OperationRequest Request { get; }

        IServiceProvider Services { get; }

        IExecutionResult? Result { get; set; }
    }
}
