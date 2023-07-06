using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace HotChocolate.Data.ExpressionNodes;

internal sealed class ExpressionCompilationContext : IExpressionCompilationContext, ICompiledExpressions
{
    private readonly ExpressionTreeCache _expressionTreeCache;
    public ChildrenExpressionCollection Children { get; }
    private readonly VariableContextWithSafety _variableContextWithSafety;

    public ExpressionCompilationContext(ExpressionTreeCache expressionTreeCache, SealedMetaTree tree)
    {
        _expressionTreeCache = expressionTreeCache;
        Children = new(new ChildList(this));
        _variableContextWithSafety = new(this);
    }

    private SealedMetaTree Tree => _expressionTreeCache.Tree;
    public IVariableContext Variables => _variableContextWithSafety;
    public ICompiledExpressions Expressions => this;

    public int NodeIndex { get; set; }
    public Identifier NodeId => Identifier.FromIndex(NodeIndex);

    private ref SealedExpressionNode NodeRef => ref Tree.Nodes[NodeIndex];

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

    // Adds some safety checks.
    // Scopes the iterators to the declared variables.
    private sealed class VariableContextWithSafety : IVariableContext
    {
        private readonly BoxesWithSafety _boxesWithSafety;
        private readonly ExpressionsWithSafety _expressionsWithSafety;

        public VariableContextWithSafety(ExpressionCompilationContext context)
        {
            _boxesWithSafety = new BoxesWithSafety(context);
            _expressionsWithSafety = new ExpressionsWithSafety(context);
        }

        public IReadOnlyDictionary<Identifier, IBox> Boxes => _boxesWithSafety;
        public IReadOnlyDictionary<Identifier, BoxExpressions> Expressions => _expressionsWithSafety;

        private sealed class BoxesWithSafety : IReadOnlyDictionary<Identifier, IBox>
        {
            private readonly ExpressionCompilationContext _context;

            public BoxesWithSafety(ExpressionCompilationContext context)
            {
                _context = context;
            }

            private IReadOnlyDictionary<Identifier, IBox> Impl => _context._expressionTreeCache.Variables.Boxes;
            private StructuralDependencies Dependencies => _context.NodeRef.Dependencies.Structural;
            private bool CanAccess(Identifier id) => Dependencies.VariableIds?.Contains(id) ?? true;

            private void MakeSureCanAccess(Identifier id)
            {
                if (!CanAccess(id))
                    throw new InvalidOperationException($"The variable {id} is not accessible from this node.");
            }

            public IEnumerator<KeyValuePair<Identifier, IBox>> GetEnumerator()
            {
                var impl = Impl;
                if (Dependencies.Unspecified)
                {
                    foreach (var pair in impl)
                        yield return pair;
                }

                foreach (var id in Dependencies.VariableIds!)
                    yield return new KeyValuePair<Identifier, IBox>(id, impl[id]);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public int Count => Impl.Count;

            public bool ContainsKey(Identifier key) => CanAccess(key) && Impl.ContainsKey(key);

#pragma warning disable CS8601 // Why??
            public bool TryGetValue(Identifier key, out IBox value)
            {
                value = null!;
                return CanAccess(key) && Impl.TryGetValue(key, out value);
            }
#pragma warning restore CS8601

            public IBox this[Identifier key]
            {
                get
                {
                    MakeSureCanAccess(key);
                    return Impl[key];
                }
            }

            public IEnumerable<Identifier> Keys
            {
                get
                {
                    var deps = Dependencies;
                    if (deps.Unspecified)
                        return Impl.Keys;
                    return deps.VariableIds!;
                }
            }

            public IEnumerable<IBox> Values
            {
                get
                {
                    var deps = Dependencies;
                    if (deps.Unspecified)
                        return Impl.Values;
                    return deps.VariableIds!.Select(id => Impl[id]);
                }
            }
        }

        private sealed class ExpressionsWithSafety : IReadOnlyDictionary<Identifier, BoxExpressions>
        {
            private readonly ExpressionCompilationContext _context;

            public ExpressionsWithSafety(ExpressionCompilationContext context)
            {
                _context = context;
            }

            private IReadOnlyDictionary<Identifier, BoxExpressions> Impl => _context._expressionTreeCache.Variables.Expressions;
            private bool CanAccess(Identifier _ = default) => _context.NodeRef.Dependencies.HasExpressionDependencies;

            private void MakeSureCanAccess(Identifier id)
            {
                if (!CanAccess(id))
                    throw new InvalidOperationException($"The variable {id} is not accessible from this node.");
            }

            public IEnumerator<KeyValuePair<Identifier, BoxExpressions>> GetEnumerator()
            {
                if (!CanAccess())
                    return Enumerable.Empty<KeyValuePair<Identifier, BoxExpressions>>().GetEnumerator();
                return Impl.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public int Count => CanAccess() ? Impl.Count : 0;
            public bool ContainsKey(Identifier key) => CanAccess(key) && Impl.ContainsKey(key);

            public bool TryGetValue(Identifier key, out BoxExpressions value)
            {
                value = default;
                return CanAccess(key) && Impl.TryGetValue(key, out value);
            }

            public BoxExpressions this[Identifier key]
            {
                get
                {
                    MakeSureCanAccess(key);
                    return Impl[key];
                }
            }

            public IEnumerable<Identifier> Keys => CanAccess() ? Impl.Keys : Enumerable.Empty<Identifier>();
            public IEnumerable<BoxExpressions> Values => CanAccess() ? Impl.Values : Enumerable.Empty<BoxExpressions>();
        }
    }
}
