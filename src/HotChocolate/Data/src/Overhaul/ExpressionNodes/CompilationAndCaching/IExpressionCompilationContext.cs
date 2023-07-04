using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace HotChocolate.Data.ExpressionNodes;

public interface IExpressionCompilationContext
{
    Identifier NodeId { get; }
    Type ExpectedExpressionType { get; }
    ICompiledExpressions Expressions { get; }
    IVariableContext Variables { get; }
}

internal sealed class ExpressionCompilationContext : IExpressionCompilationContext, ICompiledExpressions
{
    private readonly Cache _cache;
    private readonly SealedMetaTree _tree;
    private readonly ChildList _children;

    public ExpressionCompilationContext(Cache cache, SealedMetaTree tree)
    {
        _cache = cache;
        _tree = tree;
        _children = new ChildList(this);
    }

    public IVariableContext Variables => _cache.Variables;
    public ICompiledExpressions Expressions => this;

    public int NodeIndex { get; set; }
    public Identifier NodeId => Identifier.FromIndex(NodeIndex);

    private ref SealedExpressionNode NodeRef => ref _tree.Nodes[NodeIndex];

    // TODO:
    public Type ExpectedExpressionType => null!;

    public Expression? Instance
    {
        get
        {
            if (NodeRef.Scope is { } scope)
                return _cache.CachedExpressions[scope.OutermostInstance.Id.AsIndex()].Expression;
            return null;
        }
    }

    public ParameterExpression? InstanceRoot
    {
        get
        {
            if (NodeRef.Scope is { } scope)
                return (ParameterExpression) _cache.CachedExpressions[scope.InnermostInstance.Id.AsIndex()].Expression;
            return null;
        }
    }

    public IReadOnlyList<Expression> Children => _children;

    private sealed class ChildList : IReadOnlyList<Expression>
    {
        private readonly ExpressionCompilationContext _context;

        public ChildList(ExpressionCompilationContext context)
        {
            _context = context;
        }

        public int Count => _context.NodeRef.Children.Count;

        public Expression this[int index]
        {
            get
            {
                var i = _context.NodeRef.Children[index].Id.AsIndex();
                return _context._cache.CachedExpressions[i].Expression;
            }
        }

        public IEnumerator<Expression> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

public struct CachedExpression
{
    public Expression Expression { get; set; }
}

public interface ICompiledExpressions
{
    // These are null in the case when we're handling a instance node ourselves.
    // I've current set it such that the scope is null when inside an instance node of a scope.
    Expression? Instance { get; }
    ParameterExpression? InstanceRoot { get; }

    IReadOnlyList<Expression> Children { get; }
}

public readonly struct Variable
{
    public object? Value { get; }
    public Type Type { get; }

    public Variable(object? value, Type type)
    {
        Value = value;
        Type = type;
    }
}

internal sealed class Cache
{
    public CachedExpression[] CachedExpressions { get; }
    public IVariableContext Variables { get; }
    public HashSet<Identifier> ValuesChanged { get; } = new();
    public bool AllValuesChanged { get; set; }
    public ExpressionCompilationContext Context { get; internal set; } = null!;

    public Cache(
        CachedExpression[] cachedExpressions,
        IVariableContext variables)
    {
        CachedExpressions = cachedExpressions;
        Variables = variables;
    }
}

public sealed class ProjectionExpressionCompiler
{
    // In this model, the caching is done per tree.
    public SealedMetaTree Tree { get; }
    private ConcurrentStack<Cache> _cache = new();

    public ProjectionExpressionCompiler(SealedMetaTree tree)
    {
        Tree = tree;
    }

    public LambdaExpression GetExpression(IReadOnlyCollection<(Identifier, Variable)> variables)
    {
        var cache = GetCache(variables);

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

        var rootExpression = expressions[^1].Expression;
        var innermostInstanceIndex = Tree.Root.Scope!.InnermostInstance.Id.AsIndex();
        var innermostInstance = (ParameterExpression) expressions[innermostInstanceIndex].Expression;
        return Expression.Lambda(rootExpression, innermostInstance);
    }

    private Cache GetCache(IReadOnlyCollection<(Identifier, Variable)> variables)
    {
        if (GetMostSuitableCache() is not { } cache)
            return CreateCache(variables);

        var boxes = cache.Variables.Boxes;
        var valuesChanged = cache.ValuesChanged;
        valuesChanged.Clear();
        foreach (var (id, variable) in variables)
        {
            bool changed = boxes[id].UpdateValue(variable.Value);
            if (changed)
                valuesChanged.Add(id);
        }

        return cache;
    }

    private Cache CreateCache(IReadOnlyCollection<(Identifier, Variable)> variables)
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

        var cache = new Cache(cachedExpressions, variableContext);
        cache.Context = new ExpressionCompilationContext(cache, Tree);

        return cache;
    }

    // For now let's just return the first cache
    private Cache? GetMostSuitableCache()
    {
        if (_cache.TryPop(out var cache))
            return cache;
        return null;
    }
}
