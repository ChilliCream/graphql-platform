using System;

namespace HotChocolate.Execution.Processing
{
    internal interface IOperationContextOwner : IDisposable
    {
        IOperationContext OperationContext { get; }
    }
}
