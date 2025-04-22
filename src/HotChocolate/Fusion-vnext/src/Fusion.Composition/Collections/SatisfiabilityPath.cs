using System.Collections;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Collections;

internal sealed class SatisfiabilityPath : IEnumerable<SatisfiabilityPathItem>
{
    private readonly Stack<SatisfiabilityPathItem> _stack = [];
    private readonly HashSet<SatisfiabilityPathItem> _hashSet = [];

    public bool Contains(SatisfiabilityPathItem item)
    {
        return _hashSet.Contains(item);
    }

    public int Count => _stack.Count;

    public bool Push(SatisfiabilityPathItem item)
    {
        if (_hashSet.Contains(item))
        {
            return false;
        }

        _stack.Push(item);
        _hashSet.Add(item);

        return true;
    }

    public SatisfiabilityPathItem Pop()
    {
        if (_stack.Count == 0)
        {
            throw new InvalidOperationException("Stack is empty.");
        }

        var item = _stack.Pop();
        _hashSet.Remove(item);

        return item;
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
    public override string ToString()
    {
        return $"{SchemaName}:{Type.Name}.{Field.Name}";
    }
}
