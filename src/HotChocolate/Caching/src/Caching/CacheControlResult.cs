using HotChocolate.Language;

namespace HotChocolate.Caching;

public class CacheControlResult
{
    public CacheControlResult(
        OperationDefinitionNode operationDefinitionNode)
    {
        OperationDefinitionNode = operationDefinitionNode;
    }

    public int? MaxAge { get; internal set; }

    public CacheControlScope Scope { get; internal set; }

    public OperationDefinitionNode OperationDefinitionNode { get; }
}