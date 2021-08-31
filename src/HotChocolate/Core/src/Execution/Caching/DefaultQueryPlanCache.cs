using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing.Plan;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Caching
{
    internal sealed class DefaultQueryPlanCache : IQueryPlanCache
    {
        private readonly Cache<QueryPlan> _cache;

        public DefaultQueryPlanCache(int capacity = 100)
        {
            _cache = new Cache<QueryPlan>(capacity);
        }

        public int Capacity => _cache.Size;

        public int Count => _cache.Usage;

        public void TryAddQueryPlan(string operationId, QueryPlan queryPlan) =>
            _cache.GetOrCreate(operationId, () => queryPlan);

        public bool TryGetQueryPlan(
            string operationId,
            [NotNullWhen(true)] out QueryPlan? queryPlan) =>
            _cache.TryGet(operationId, out queryPlan!);

        public void Clear() => _cache.Clear();
    }
}
