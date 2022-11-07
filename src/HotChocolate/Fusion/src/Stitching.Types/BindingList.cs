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

        if (!_bindings.TryGetValue(item.Target, out IBinding[]? bindings))
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
        if (_bindings.TryGetValue(target, out IBinding[]? b))
        {
            bindings = b;
            return true;
        }

        bindings = null;
        return false;
    }

    public bool Contains(IBinding item)
    {
        if (_bindings.TryGetValue(item.Target, out IBinding[]? bindings))
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
        foreach (IBinding[] bindings in _bindings.Values)
        {
            foreach (IBinding binding in bindings)
            {
                array[i++] = binding;
            }
        }
    }

    public void CopyTo(BindingList bindings)
    {
        foreach (KeyValuePair<SchemaCoordinate, IBinding[]> item in _bindings)
        {
            if (!bindings._bindings.TryGetValue(item.Key, out IBinding[]? b))
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
        foreach (IBinding[] bindings in _bindings.Values)
        {
            foreach (IBinding binding in bindings)
            {
                yield return binding;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
