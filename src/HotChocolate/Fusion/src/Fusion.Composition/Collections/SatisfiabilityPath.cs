using System.Collections;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Collections;

internal sealed class SatisfiabilityPath : IEnumerable<SatisfiabilityPathItem>
{
    private readonly Stack<SatisfiabilityPathItem> _stack = [];
    private readonly Dictionary<SatisfiabilityPathItem, int> _depthByItem = [];
    private int _minCollisionDepth = int.MaxValue;

    public bool Contains(SatisfiabilityPathItem item)
    {
        return _depthByItem.ContainsKey(item);
    }

    public int Count => _stack.Count;

    public bool Push(SatisfiabilityPathItem item)
    {
        if (_depthByItem.TryGetValue(item, out var existingDepth))
        {
            if (existingDepth < _minCollisionDepth)
            {
                _minCollisionDepth = existingDepth;
            }

            return false;
        }

        _depthByItem[item] = _stack.Count;
        _stack.Push(item);

        return true;
    }

    /// <summary>
    /// Begins a scope that measures the shallowest cycle collision that occurs until the matching
    /// <see cref="EndCollisionScope"/> call. Returns the enclosing scope's state to restore later.
    /// </summary>
    public int BeginCollisionScope()
    {
        var previous = _minCollisionDepth;
        _minCollisionDepth = int.MaxValue;

        return previous;
    }

    /// <summary>
    /// Ends the current collision scope, returning the shallowest collision depth observed within
    /// it. The enclosing scope is restored and also sees that collision so an inherited collision
    /// propagates outward.
    /// </summary>
    /// <param name="previous">The value returned by the matching <see cref="BeginCollisionScope"/>.</param>
    public int EndCollisionScope(int previous)
    {
        var observed = _minCollisionDepth;
        _minCollisionDepth = Math.Min(previous, observed);

        return observed;
    }

    public SatisfiabilityPathItem Pop()
    {
        if (_stack.Count == 0)
        {
            throw new InvalidOperationException("Stack is empty.");
        }

        var item = _stack.Pop();
        _depthByItem.Remove(item);

        return item;
    }

    public SatisfiabilityPathItem Peek()
    {
        if (_stack.Count == 0)
        {
            throw new InvalidOperationException("Stack is empty.");
        }

        return _stack.Peek();
    }

    public bool TryPeek([MaybeNullWhen(false)] out SatisfiabilityPathItem item)
    {
        if (_stack.Count == 0)
        {
            item = null;
            return false;
        }

        item = _stack.Peek();
        return true;
    }

    public void Clear()
    {
        _stack.Clear();
        _depthByItem.Clear();
    }

    public IEnumerator<SatisfiabilityPathItem> GetEnumerator()
    {
        return _stack.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public override string ToString()
    {
        return string.Join(" -> ", _stack.Reverse());
    }
}

internal sealed record SatisfiabilityPathItem(
    MutableOutputFieldDefinition Field,
    MutableComplexTypeDefinition Type,
    string SchemaName)
{
    public ITypeDefinition FieldType { get; } = Field.Type.AsTypeDefinition();

    public SelectionSetNode? ProvidedSelectionSet { get; init; }

    private readonly int _hashCode = HashCode.Combine(Field, Type, SchemaName);

    public override string ToString()
    {
        return $"{SchemaName}:{Type.Name}.{Field.Name}<{FieldType.Name}>";
    }

    public override int GetHashCode() => _hashCode;
}
