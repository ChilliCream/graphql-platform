using System;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing
{
    internal class OperationContextPoolPolicy
        : IPooledObjectPolicy<OperationContext>
    {
        private readonly Func<OperationContext> _factory;

        public OperationContextPoolPolicy(Func<OperationContext> factory)
        {
            _factory = factory;
        }

        public OperationContext Create() => _factory();

        public bool Return(OperationContext obj)
        {
            obj.Clean();
            return true;
        }
    }
}
