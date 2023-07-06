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

    public Expression GetRootExpression()
    {
        var nodeIndex = Cache.Tree.RootNodeIndex;
        return GetExpressionForNode(nodeIndex);
    }

    public Expression GetExpression(Identifier selectionId)
    {
        var nodeIndex = Cache.Tree.SelectionIdToOuterNode[selectionId].AsIndex();
        return GetExpressionForNode(nodeIndex);
    }

    private Expression GetExpressionForNode(int nodeIndex)
    {
        CacheExpressions();
        return Cache.CachedExpressions[nodeIndex].Expression;
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
        if (HaveVariablesBeenProcessed)
            return;

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
        // NOTE: It might be faster to do the caching in a tree like fashion,
        //       at least in the case where we don't recache everything.
        //       But it's not exactly clear if it will actually at a glance.
        //       It does in theory help avoid most of the checks,
        //       because they would be done at an earlier stage in the tree,
        //       cutting all the whole branches, but cache misses would likely kill the benefits.
        if (Cache.IsFirstUse)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i];
                // The ones that have no dependencies are computed
                if (!node.Dependencies.HasNoDependencies)
                    CacheNode(i);
            }

            Cache.IsFirstUse = false;
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

        HaveVariablesBeenProcessed = true;
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

    public BorrowedProjectionExpressionCache LeaseCache() => GetCache();

    internal void ReturnCache(BorrowedProjectionExpressionCache lease)
    {
        // TODO: error out if already returned.
        _caches.Push(lease);
    }

    private BorrowedProjectionExpressionCache GetCache()
    {
        if (GetMostSuitableCache() is not { } cacheLease)
            return CreateCache();

        cacheLease.Cache.ValuesChanged.Clear();
        cacheLease.HaveVariablesBeenProcessed = false;

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
