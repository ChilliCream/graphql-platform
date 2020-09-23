using System;

namespace HotChocolate.Execution
{
    internal interface IOperationContextOwner : IDisposable
    {
        IOperationContext OperationContext { get; }
    }
}
