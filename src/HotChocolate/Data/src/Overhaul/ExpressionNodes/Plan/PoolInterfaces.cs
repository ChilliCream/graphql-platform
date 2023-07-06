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
        node.OutermostNode = node;
        return node;
    }

    public static Scope CreateScopeWithInstance(
        this ExpressionPools pools,
        ExpressionNode? declaringNode = null)
    {
        var instance = pools.ExpressionNodePool.CreateInnermost(InstanceExpressionFactory.Instance);
        var scope = pools.ScopePool.Get();
        scope.InnerInstance = instance;
        scope.DeclaringNode = declaringNode;
        return scope;
    }
}

// TEMP:
public sealed class ExpressionNodePool : IExpressionNodePool
{
    private readonly HashSet<ExpressionNode> _notReturned = new();

    public ExpressionNode Create(IExpressionFactory factory)
    {
        var node = new ExpressionNode(factory);
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

public class ScopePool : IObjectPool<Scope>
{
    private readonly HashSet<Scope> _notReturned = new();

    public Scope Get()
    {
        var scope = new Scope();
        _notReturned.Add(scope);
        return scope;
    }

    public bool Return(Scope scope) => _notReturned.Remove(scope);
}

public sealed record ExpressionPools(
    IExpressionNodePool ExpressionNodePool,
    IObjectPool<Scope> ScopePool);
