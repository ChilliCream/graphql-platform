using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Caching
{
    internal sealed class DefaultPreparedOperationCache : IPreparedOperationCache
    {
        private readonly Cache<IPreparedOperation> _cache;

        public DefaultPreparedOperationCache(int capacity = 100)
        {
            _cache = new Cache<IPreparedOperation>(capacity);
        }

        public void TryAddOperation(
            string operationId,
            IPreparedOperation operation) =>
            _cache.GetOrCreate(operationId, () => operation);

        public bool TryGetOperation(
            string operationId,
            [NotNullWhen(true)] out IPreparedOperation? operation) =>
            _cache.TryGet(operationId, out operation!);

        public void Clear() => _cache.Clear();
    }
}

