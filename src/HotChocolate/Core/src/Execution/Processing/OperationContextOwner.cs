using System;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing
{
    internal class OperationContextOwner : IOperationContextOwner
    {
        private readonly OperationContext _operationContext;
        private readonly ObjectPool<OperationContext> _operationContextPool;
        private bool _disposed;

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
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(OperationContextOwner));
                }

                return _operationContext;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _operationContextPool.Return(_operationContext);
                _disposed = false;
            }
        }
    }
}
