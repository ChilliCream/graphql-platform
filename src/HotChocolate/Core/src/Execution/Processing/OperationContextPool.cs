using System;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing
{
    internal sealed class OperationContextPool 
        : DefaultObjectPool<OperationContext>
    {
        public OperationContextPool(
            Func<OperationContext> factory,
            int maximumRetained = 16)
            : base(new OperationContextPoolPolicy(factory), maximumRetained)
        {
        }

        private class OperationContextPoolPolicy 
            : IPooledObjectPolicy<OperationContext>
        {
            private Func<OperationContext> _factory;

            public OperationContextPoolPolicy(Func<OperationContext> factory)
            {
                _factory = factory;
            }

            public OperationContext Create() => _factory();

            public bool Return(OperationContext obj)
            {
                obj.Reset();
                return true;
            }
        }
    }
}