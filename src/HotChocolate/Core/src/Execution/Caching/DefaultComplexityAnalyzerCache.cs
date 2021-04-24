using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Pipeline.Complexity;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Caching
{
    internal class DefaultComplexityAnalyzerCache : IComplexityAnalyzerCache
    {
        private readonly Cache<ComplexityAnalyzerDelegate> _cache;

        public DefaultComplexityAnalyzerCache(int capacity = 100)
        {
            _cache = new Cache<ComplexityAnalyzerDelegate>(capacity);
        }

        public int Capacity => _cache.Size;

        public int Count => _cache.Usage;

        public bool TryGetOperation(
            string operationId,
            [NotNullWhen(true)] out ComplexityAnalyzerDelegate? analyzer) =>
            _cache.TryGet(operationId, out analyzer);


        public void TryAddOperation(
            string operationId,
            ComplexityAnalyzerDelegate analyzer) =>
            _cache.GetOrCreate(operationId, () => analyzer);

        public void Clear() => _cache.Clear();
    }
}
