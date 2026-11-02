using System.Collections;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types;
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
        if (!_hashSet.Add(item))
        {
            return false;
        }

        _stack.Push(item);

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
        _hashSet.Clear();
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

    /// <summary>
    /// Gets a value indicating whether <see cref="ProvidedSelectionSet"/> originates from an event
    /// stream message rather than an <c>@provides</c> selection. Event stream messages genuinely gate
    /// satisfiability; <c>@provides</c> is an optimization that never makes a path satisfiable.
    /// </summary>
    public bool ProvidedByEventStream { get; init; }

    private readonly int _hashCode = HashCode.Combine(Field, Type, SchemaName);

    public override string ToString()
    {
        return $"{SchemaName}:{Type.Name}.{Field.Name}<{FieldType.Name}>";
    }

    public override int GetHashCode() => _hashCode;
}
