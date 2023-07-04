namespace HotChocolate.Data.ExpressionNodes;

public interface IExpressionNodePool
{
    // TODO:
    ExpressionNode Get(IExpressionFactory factory);
    void Return(ExpressionNode node);
}

public interface IObjectPool<T>
{
    T Get();
    // Returns false if it's already been returned.
    bool Return(T item);
}

public sealed record ExpressionPools(
    IExpressionNodePool ExpressionNodePool,
    IObjectPool<Scope> ScopePool);
