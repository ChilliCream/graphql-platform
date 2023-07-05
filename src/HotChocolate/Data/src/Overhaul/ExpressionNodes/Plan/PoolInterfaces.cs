using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Data.ExpressionNodes;

public interface IExpressionNodePool
{
    ExpressionNode Create(IExpressionFactory factory);
    void Return(ExpressionNode node);
}

public static class ExpressionNodeCreation
{
    public static ExpressionNode CreateInnermost(
        this IExpressionNodePool pool,
        IExpressionFactory factory)
    {
        var node = pool.Create(factory);
        node.IsInnermost = true;
        node.InnermostOrOutermostNode = node;
        return node;
    }
}

// TEMP:
public sealed class ExpressionNodePool : IExpressionNodePool
{
    private readonly HashSet<ExpressionNode> _notReturned = new();

    public ExpressionNode Create(IExpressionFactory factory)
    {
        var node = new ExpressionNode { ExpressionFactory = factory };
        node.OwnDependencies = DependencyHelper.GetDependencies(factory);
        _notReturned.Add(node);
        return node;
    }

    public void Return(ExpressionNode node)
    {
        _notReturned.Remove(node);
    }

    public void EnsureNoLeaks()
    {
        if (_notReturned.Any())
            throw new Exception("There are leaked expression nodes.");
    }
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
