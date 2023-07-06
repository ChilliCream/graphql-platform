using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace HotChocolate.Data.ExpressionNodes;

public sealed class BorrowedProjectionExpressionCache : IDisposable
{
    internal ExpressionTreeCache Cache { get; }
    internal ProjectionExpressionCacheManager Manager { get; }
    internal bool HaveVariablesBeenProcessed { get; set; }

    internal BorrowedProjectionExpressionCache(
        ExpressionTreeCache cache,
        ProjectionExpressionCacheManager manager)
    {
        Cache = cache;
        Manager = manager;
    }

    public LambdaExpression GetRootExpression()
    {
        var nodeIndex = Cache.Tree.RootNodeIndex;
        return GetExpressionForNode(nodeIndex);
    }

    public LambdaExpression GetExpression(Identifier selectionId)
    {
        var nodeIndex = Cache.Tree.SelectionIdToOuterNode[selectionId].AsIndex();
        return GetExpressionForNode(nodeIndex);
    }

    private LambdaExpression GetExpressionForNode(int nodeIndex)
    {
        var expression = Cache.CachedExpressions[nodeIndex].Expression;
        var innermostInstanceIndex = Cache.Tree.Nodes[nodeIndex].Scope!.InnermostInstance.AsIndex();
        var innermostInstance = (ParameterExpression) Cache.CachedExpressions[innermostInstanceIndex].Expression;
        return Expression.Lambda(expression, innermostInstance);
    }

    public void SetVariableValue<T>(Identifier id, T value)
        where T : IEquatable<T>
    {
#if false
        // this should be done once and somewhere else I think.
        Box<T> box;
        var boxes = Cache.Variables.Boxes;
        if (!boxes.TryGetValue(id, out var ibox))
        {
            Cache.VersionsBefore.Add((id, 0));
            box = new Box<T> { Value = value };
            boxes.Add(id, box);
        }
        else
        {
            box = (Box<T>) ibox;
        }
#else
        var box = (Box<T>) Cache.Variables.Boxes[id];
#endif

        // Is thread safety actually needed here?
        lock (box)
        {
            bool changed = box.UpdateValue(value);
            if (changed)
                Cache.ValuesChanged.Add(id);


        }
    }

    private void CacheExpressions()
    {
        var expressions = Cache.CachedExpressions;
        var context = Cache.Context;
        var valuesChanged = Cache.ValuesChanged;
        var nodes = Cache.Tree.Nodes;

        void CacheNode(int i)
        {
            var node = nodes[i];
            context.NodeIndex = i;
            expressions[i].Expression = node.ExpressionFactory.GetExpression(context);
        }

        // bypass the checks on first use
        if (Cache.IsFirstUse)
        {
            for (int i = 0; i < nodes.Length; i++)
                CacheNode(i);
        }
        else if (valuesChanged.Count > 0)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i];
                var structuralDeps = node.Dependencies.Structural;
                if (structuralDeps.Unspecified || valuesChanged.Overlaps(structuralDeps.VariableIds!))
                    CacheNode(i);
            }
        }
    }

    public void Dispose()
    {
        Manager.ReturnCache(this);
    }
}

public sealed class ProjectionExpressionCacheManager
{
    // In this model, the caching is done per tree.
    private ExpressionTreeCache _referenceCache;
    private ConcurrentStack<BorrowedProjectionExpressionCache> _caches = new();

    internal ProjectionExpressionCacheManager(ExpressionTreeCache referenceCache)
    {
        _referenceCache = referenceCache;
    }

    public BorrowedProjectionExpressionCache LeaseCache()
    {
        var cacheLease = GetCache();
        var cache = cacheLease.Cache;

        return cacheLease;
    }

    internal void ReturnCache(BorrowedProjectionExpressionCache lease)
    {
        // TODO: error out if already returned.
        _caches.Push(lease);
    }

    private BorrowedProjectionExpressionCache GetCache()
    {
        if (GetMostSuitableCache() is not { } cacheLease)
            return CreateCache();

        var cache = cacheLease.Cache;
        var valuesChanged = cache.ValuesChanged;
        valuesChanged.Clear();

        return cacheLease;
    }

    // TODO: This code should only be used once. Pull it out somewhere.
    private void CreateBoxes(IReadOnlyCollection<(Identifier, Variable)> variables)
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
    }


    private BorrowedProjectionExpressionCache CreateCache()
    {
        // Copy boxes
        var referenceBoxes = _referenceCache.Variables.Boxes;
        var newBoxes = new Dictionary<Identifier, IBox>(referenceBoxes.Count);
        var newBoxExpressions = new Dictionary<Identifier, BoxExpressions>(referenceBoxes.Count);
        foreach (var (key, box) in referenceBoxes)
        {
            var boxCopy = box.Clone();
            newBoxes.Add(key, boxCopy);
            var boxExpressions = BoxExpressions.Create(boxCopy);
            newBoxExpressions.Add(key, boxExpressions);
        }

        var cachedExpressions = _referenceCache.CachedExpressions.ToArray();
        var variableContext = new VariableContext(newBoxExpressions, newBoxes);

        var cache = new ExpressionTreeCache(_referenceCache.Tree, cachedExpressions, variableContext);
        cache.IsFirstUse = false;

        var context = cache.Context;
        var nodes = _referenceCache.Tree.Nodes;
        for (int i = 0; i < nodes.Length; i++)
        {
            var node = nodes[i];
            // This allows us to recompute all nodes that depend on boxes,
            // while simply copying all the ones that don't.
            // The ones that don't hence end up being only cached once per tree.
            if (node.Dependencies.HasExpressionDependencies)
            {
                context.NodeIndex = i;
                cachedExpressions[i].Expression = node.ExpressionFactory.GetExpression(context);
            }
        }

        var cacheLease = new BorrowedProjectionExpressionCache(cache, this);
        return cacheLease;
    }

    // For now let's just return the first cache
    private BorrowedProjectionExpressionCache? GetMostSuitableCache()
    {
        if (_caches.TryPop(out var cache))
            return cache;
        return null;
    }
}
