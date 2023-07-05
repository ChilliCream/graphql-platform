using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace HotChocolate.Data.ExpressionNodes;

public sealed class ProjectionExpressionCompiler
{
    // In this model, the caching is done per tree.
    public SealedMetaTree Tree { get; }
    private ConcurrentStack<ExpressionTreeCache> _cache = new();

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
        var innermostInstanceIndex = Tree.Root.Scope!.InnermostInstance.AsIndex();
        var innermostInstance = (ParameterExpression) expressions[innermostInstanceIndex].Expression;
        return Expression.Lambda(rootExpression, innermostInstance);
    }

    private ExpressionTreeCache GetCache(IReadOnlyCollection<(Identifier, Variable)> variables)
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

    private ExpressionTreeCache CreateCache(IReadOnlyCollection<(Identifier, Variable)> variables)
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

        return cache;
    }

    // For now let's just return the first cache
    private ExpressionTreeCache? GetMostSuitableCache()
    {
        if (_cache.TryPop(out var cache))
            return cache;
        return null;
    }
}
