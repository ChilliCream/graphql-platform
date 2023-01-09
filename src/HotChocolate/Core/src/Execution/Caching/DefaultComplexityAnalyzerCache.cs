using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Pipeline.Complexity;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Caching;

internal sealed class DefaultComplexityAnalyzerCache : IComplexityAnalyzerCache
{
    private readonly Cache<ComplexityAnalyzerDelegate> _cache;

    public DefaultComplexityAnalyzerCache(int capacity = 100)
    {
        _cache = new Cache<ComplexityAnalyzerDelegate>(capacity);
    }

    public int Capacity => _cache.Capacity;

    public int Count => _cache.Usage;

    public bool TryGetAnalyzer(
        string operationId,
        [NotNullWhen(true)] out ComplexityAnalyzerDelegate? analyzer)
        => _cache.TryGet(operationId, out analyzer);


    public void TryAddAnalyzer(
        string operationId,
        ComplexityAnalyzerDelegate analyzer)
        => _cache.GetOrCreate(operationId, () => analyzer);

    public void Clear() => _cache.Clear();
}
