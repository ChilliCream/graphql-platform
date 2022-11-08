using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Stitching.Types;

internal sealed class BindingList : ICollection<IBinding>
{
    private readonly Dictionary<SchemaCoordinate, IBinding[]> _bindings = new();
    private int _count;

    public int Count => _count;

    public bool IsReadOnly => false;

    public void Add(IBinding item)
    {
        _count++;

        if (!_bindings.TryGetValue(item.Target, out var bindings))
        {
            bindings = new[] { item };
            _bindings.Add(item.Target, bindings);
            return;
        }

        var index = bindings.Length;
        Array.Resize(ref bindings, index + 1);
        bindings[index] = item;
        _bindings[item.Target] = bindings;
    }

    public bool Remove(IBinding item)
        => throw new NotSupportedException();

    public bool TryGetBindings(
        SchemaCoordinate target,
        [NotNullWhen(true)] out IReadOnlyList<IBinding>? bindings)
    {
        if (_bindings.TryGetValue(target, out var b))
        {
            bindings = b;
            return true;
        }

        bindings = null;
        return false;
    }

    public bool Contains(IBinding item)
    {
        if (_bindings.TryGetValue(item.Target, out var bindings))
        {
            return bindings.Length is 0
                ? bindings[0].Equals(item)
                : Array.Exists(bindings, b => b.Equals(item));
        }

        return false;
    }

    public void Clear() => _bindings.Clear();

    public void CopyTo(IBinding[] array, int arrayIndex)
    {
        var i = arrayIndex;
        foreach (var bindings in _bindings.Values)
        {
            foreach (var binding in bindings)
            {
                array[i++] = binding;
            }
        }
    }

    public void CopyTo(BindingList bindings)
    {
        foreach (var item in _bindings)
        {
            if (!bindings._bindings.TryGetValue(item.Key, out var b))
            {
                var copy = new IBinding[item.Value.Length];
                Array.Copy(item.Value, 0, copy, 0, copy.Length);
                bindings._bindings.Add(item.Key, copy);
                bindings._count += item.Value.Length;
                return;
            }

            var start = b.Length;
            Array.Resize(ref b, start + item.Value.Length);
            Array.Copy(item.Value, 0, b, start, item.Value.Length);
            bindings._bindings[item.Key] = b;
            bindings._count += item.Value.Length;
        }
    }

    public IEnumerator<IBinding> GetEnumerator()
    {
        foreach (var bindings in _bindings.Values)
        {
            foreach (var binding in bindings)
            {
                yield return binding;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
