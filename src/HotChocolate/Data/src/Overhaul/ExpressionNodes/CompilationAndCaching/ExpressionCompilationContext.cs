using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace HotChocolate.Data.ExpressionNodes;

internal sealed class ExpressionCompilationContext : IExpressionCompilationContext, ICompiledExpressions
{
    private readonly ExpressionTreeCache _expressionTreeCache;
    private readonly SealedMetaTree _tree;
    public ChildrenExpressionCollection Children { get; }

    public ExpressionCompilationContext(ExpressionTreeCache expressionTreeCache, SealedMetaTree tree)
    {
        _expressionTreeCache = expressionTreeCache;
        _tree = tree;
        Children = new(new ChildList(this));
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

    // Has to be a class, because we're using the IReadOnlyList abstraction for iteration.
    // This being a class avoids boxing.
    private sealed class ChildList : IReadOnlyList<Expression>
    {
        private readonly ExpressionCompilationContext _context;

        public ChildList(ExpressionCompilationContext context)
        {
            _context = context;
        }

        public int Count => _context.NodeRef.Children.Length;

        public Expression this[int index]
        {
            get
            {
                var i = _context.NodeRef.Children[index].AsIndex();
                return _context._expressionTreeCache.CachedExpressions[i].Expression;
            }
        }

        public IEnumerator<Expression> GetEnumerator() => throw new NotSupportedException();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
