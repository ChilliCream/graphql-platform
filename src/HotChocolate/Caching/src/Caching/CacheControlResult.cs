using HotChocolate.Language;

namespace HotChocolate.Caching;

public class CacheControlResult
{
    private int? _maxAge;

    internal CacheControlResult(OperationDefinitionNode operationDefinitionNode)
    {
        OperationDefinitionNode = operationDefinitionNode;
    }

    public int MaxAge { get => _maxAge ?? 0; internal set => _maxAge = value; }

    public CacheControlScope Scope { get; internal set; }

    internal OperationDefinitionNode OperationDefinitionNode { get; }

    internal bool MaxAgeHasValue => _maxAge.HasValue;
}