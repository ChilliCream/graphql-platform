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

    ChildrenExpressionCollection Children { get; }
}

// This is necessary to be able to foreach without allocations.
// This has to be a class to be able to use linq without boxing of the enumerable
// (still with boxing of the enumerator).
public sealed class ChildrenExpressionCollection : IReadOnlyList<Expression>
{
    private readonly IReadOnlyList<Expression> _expressions;

    public ChildrenExpressionCollection(IReadOnlyList<Expression> expressions)
    {
        _expressions = expressions;
    }

    public int Count => _expressions.Count;
    public Expression this[int index] => _expressions[index];

    public struct Enumerator : IEnumerator<Expression>
    {
        private readonly IReadOnlyList<Expression> _expressions;
        private int _index;

        public Enumerator(IReadOnlyList<Expression> expressions)
        {
            _expressions = expressions;
            _index = -1;
        }

        public bool MoveNext()
        {
            _index++;
            return _index < _expressions.Count;
        }

        public void Reset() => _index = -1;
        public readonly Expression Current => _expressions[_index];

        object IEnumerator.Current => Current;
        public void Dispose()
        {
        }
    }

    public IEnumerator<Expression> GetEnumerator() => new Enumerator(_expressions);
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
