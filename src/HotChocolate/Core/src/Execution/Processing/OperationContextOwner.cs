using System;
using System.Threading;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing
{
    internal class OperationContextOwner : IOperationContextOwner
    {
        private readonly OperationContext _operationContext;
        private readonly ObjectPool<OperationContext> _operationContextPool;
        private int _disposed;

        public OperationContextOwner(
            OperationContext operationContext,
            ObjectPool<OperationContext> operationContextPool)
        {
            _operationContext = operationContext;
            _operationContextPool = operationContextPool;
        }

        public IOperationContext OperationContext
        {
            get
            {
                if (_disposed == 1)
                {
                    throw new ObjectDisposedException(nameof(OperationContextOwner));
                }

                return _operationContext;
            }
        }

        public void Dispose()
        {
            if (_disposed == 0)
            {
                if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
                {
                    _operationContextPool.Return(_operationContext);
                }
            }
        }
    }
}
