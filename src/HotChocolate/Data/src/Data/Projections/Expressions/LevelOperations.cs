using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace HotChocolate.Data.Projections.Expressions;

public class LevelOperations : IEnumerable<Expression>
{
    private readonly List<Expression> _operations = new();

    // This is to support the queue behavior.
    private int _startIndex = 0;

    public void Enqueue(Expression expression) => _operations.Add(expression);
    public Expression Dequeue() => _operations[_startIndex++];
    public Expression Peek() => _operations[_startIndex];
    public bool TryPeek(out Expression expression)
    {
        if (_startIndex < _operations.Count)
        {
            expression = _operations[_startIndex];
            return true;
        }

        expression = default!;
        return false;
    }
    public int Count => _operations.Count - _startIndex;

    public Expression this[int index]
    {
        get => _operations[_startIndex + index];
        set => _operations[_startIndex + index] = value;
    }

    public IEnumerator<Expression> GetEnumerator()
    {
        return _operations.Skip(_startIndex).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
