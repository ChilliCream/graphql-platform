using System;
using System.Collections;
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

public interface ICompiledExpressions
{
    // These are null in the case when we're handling a instance node ourselves.
    // I've current set it such that the scope is null when inside an instance node of a scope.
    Expression? Instance { get; }
    ParameterExpression? InstanceRoot { get; }

    IReadOnlyList<Expression> Children { get; }
}

internal sealed class ExpressionCompilationContext : IExpressionCompilationContext, ICompiledExpressions
{
    private readonly ExpressionTreeCache _expressionTreeCache;
    private readonly SealedMetaTree _tree;
    private readonly ChildList _children;

    public ExpressionCompilationContext(ExpressionTreeCache expressionTreeCache, SealedMetaTree tree)
    {
        _expressionTreeCache = expressionTreeCache;
        _tree = tree;
        _children = new ChildList(this);
    }

    public IVariableContext Variables => _expressionTreeCache.Variables;
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
            {
                int index = scope.OutermostInstance.AsIndex();
                var expression = _expressionTreeCache.CachedExpressions[index].Expression;
                return expression;
            }
            return null;
        }
    }

    public ParameterExpression? InstanceRoot
    {
        get
        {
            if (NodeRef.Scope is { } scope)
            {
                int index = scope.InnermostInstance.AsIndex();
                var expression = (ParameterExpression) _expressionTreeCache.CachedExpressions[index].Expression;
                return expression;
            }
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
                var i = _context.NodeRef.Children[index].AsIndex();
                return _context._expressionTreeCache.CachedExpressions[i].Expression;
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
