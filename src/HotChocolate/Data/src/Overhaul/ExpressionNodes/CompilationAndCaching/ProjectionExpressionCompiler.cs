using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace HotChocolate.Data.ExpressionNodes;

public sealed class BorrowedProjectionExpressionCache : IDisposable
{
    internal ExpressionTreeCache Cache { get; }
    internal ProjectionExpressionCompiler Compiler { get; }

    internal BorrowedProjectionExpressionCache(
        ExpressionTreeCache cache,
        ProjectionExpressionCompiler compiler)
    {
        Cache = cache;
        Compiler = compiler;
    }

    public LambdaExpression GetRootExpression()
    {
        var nodeIndex = Compiler.Tree.RootNodeIndex;
        return GetExpressionForNode(nodeIndex);
    }

    public LambdaExpression GetExpression(Identifier selectionId)
    {
        var nodeIndex = Compiler.Tree.SelectionIdToOuterNode[selectionId].AsIndex();
        return GetExpressionForNode(nodeIndex);
    }

    private LambdaExpression GetExpressionForNode(int nodeIndex)
    {
        var expression = Cache.CachedExpressions[nodeIndex].Expression;
        var innermostInstanceIndex = Compiler.Tree.Nodes[nodeIndex].Scope!.InnermostInstance.AsIndex();
        var innermostInstance = (ParameterExpression) Cache.CachedExpressions[innermostInstanceIndex].Expression;
        return Expression.Lambda(expression, innermostInstance);
    }

    public void Dispose()
    {
        Compiler.ReturnCache(this);
    }
}

public sealed class ProjectionExpressionCompiler
{
    // In this model, the caching is done per tree.
    public SealedMetaTree Tree { get; }
    private ConcurrentStack<BorrowedProjectionExpressionCache> _cache = new();

    public ProjectionExpressionCompiler(SealedMetaTree tree)
    {
        Tree = tree;
    }

    public BorrowedProjectionExpressionCache LeaseCache(IReadOnlyCollection<(Identifier, Variable)> variables)
    {
        var cacheLease = GetCache(variables);
        var cache = cacheLease.Cache;

        bool allValuesChanged = cache.AllValuesChanged;
        var expressions = cache.CachedExpressions;
        var context = cache.Context;
        for (int i = 0; i < Tree.Nodes.Length; i++)
        {
            var node = Tree.Nodes[i];
            if (allValuesChanged || cache.ValuesChanged.Overlaps(node.Dependencies.VariableIds!))
            {
                context.NodeIndex = i;
                expressions[i].Expression = node.ExpressionFactory.GetExpression(context);
            }
        }

        return cacheLease;
    }

    internal void ReturnCache(BorrowedProjectionExpressionCache lease)
    {
        // TODO: error out if already returned.
        _cache.Push(lease);
    }

    private BorrowedProjectionExpressionCache GetCache(IReadOnlyCollection<(Identifier, Variable)> variables)
    {
        if (GetMostSuitableCache() is not { } cacheLease)
            return CreateCache(variables);

        var cache = cacheLease.Cache;

        var boxes = cache.Variables.Boxes;
        var valuesChanged = cache.ValuesChanged;
        valuesChanged.Clear();
        foreach (var (id, variable) in variables)
        {
            bool changed = boxes[id].UpdateValue(variable.Value);
            if (changed)
                valuesChanged.Add(id);
        }

        return cacheLease;
    }

    private BorrowedProjectionExpressionCache CreateCache(IReadOnlyCollection<(Identifier, Variable)> variables)
    {
        // TODO: Add more filtering for the non structural dependencies
        Dictionary<Identifier, IBox> boxes = new(variables.Count);
        foreach (var (id, variable) in variables)
        {
            var box = BoxHelper.Create(variable.Value, variable.Type);
            boxes.Add(id, box);
        }

        Dictionary<Identifier, BoxExpressions> expressions = new(boxes.Count);
        foreach (var (id, box) in boxes)
        {
            var e = BoxExpressions.Create(box);
            expressions.Add(id, e);
        }

        var variableContext = new VariableContext(expressions, boxes);
        var cachedExpressions = new CachedExpression[Tree.Nodes.Length];

        var cache = new ExpressionTreeCache(cachedExpressions, variableContext);
        cache.Context = new ExpressionCompilationContext(cache, Tree);

        var cacheLease = new BorrowedProjectionExpressionCache(cache, this);
        return cacheLease;
    }

    // For now let's just return the first cache
    private BorrowedProjectionExpressionCache? GetMostSuitableCache()
    {
        if (_cache.TryPop(out var cache))
            return cache;
        return null;
    }
}
