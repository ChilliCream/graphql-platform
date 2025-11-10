using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Fusion.Types.Collections;

public abstract class FusionFieldDefinitionCollection<TField>
    : IEnumerable<TField> where TField : IFusionFieldDefinition
{
    private readonly TField[] _fields;
    private readonly FrozenDictionary<string, TField> _map;
    private readonly int _length;

    protected FusionFieldDefinitionCollection(TField[] fields)
    {
        _map = fields.ToFrozenDictionary(t => t.Name);
        _length = fields.Count(t => !t.IsInaccessible);
        _fields = fields;

        Partitioner.PartitionByAccessibility(_fields);
    }

    public int Count => _length;

    public TField this[string name]
    {
        get
        {
            var field = _map[name];

            if (field.IsInaccessible)
            {
                throw new KeyNotFoundException();
            }

            return field;
        }
    }

    public TField this[int index]
    {
        get
        {
            if (index < _length)
            {
                return _fields[index];
            }

            throw new IndexOutOfRangeException();
        }
    }

    public TField GetField(
        string name,
        bool allowInaccessibleFields)
    {
        var field = _map[name];

        if (!allowInaccessibleFields && field.IsInaccessible)
        {
            throw new KeyNotFoundException();
        }

        return field;
    }

    public TField GetFieldAt(
        int index,
        bool allowInaccessibleFields)
    {
        if (allowInaccessibleFields)
        {
            return _fields[index];
        }

        if (index < _length)
        {
            return _fields[index];
        }

        throw new IndexOutOfRangeException();
    }

    public bool TryGetField(
        string name,
        [NotNullWhen(true)] out TField? field)
    {
        if (_map.TryGetValue(name, out field) && !field.IsInaccessible)
        {
            return true;
        }

        field = default;
        return false;
    }

    public bool TryGetField(
        string name,
        bool allowInaccessibleFields,
        [NotNullWhen(true)] out TField? field)
    {
        if (allowInaccessibleFields)
        {
            return _map.TryGetValue(name, out field);
        }

        if (_map.TryGetValue(name, out field) && !field.IsInaccessible)
        {
            return true;
        }

        field = default;
        return false;
    }

    public bool ContainsName(string name)
        => _map.TryGetValue(name, out var field)
            && !field.IsInaccessible;

    public bool ContainsName(string name, bool allowInaccessibleFields)
    {
        if (allowInaccessibleFields)
        {
            return _map.ContainsKey(name);
        }

        return _map.TryGetValue(name, out var field) && !field.IsInaccessible;
    }

    public FieldEnumerator AsEnumerable() => new(_fields, _length);

    public FieldEnumerator AsEnumerable(bool allowInaccessibleFields)
        => allowInaccessibleFields ? new(_fields, _fields.Length) : new(_fields, _length);

    public FieldEnumerator GetEnumerator()
        => AsEnumerable().GetEnumerator();

    IEnumerator<TField> IEnumerable<TField>.GetEnumerator()
        => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public struct FieldEnumerator : IEnumerable<TField>, IEnumerator<TField>
    {
        private readonly TField[] _fields;
        private readonly int _length;
        private int _position;

        internal FieldEnumerator(TField[] fields, int length)
        {
            Debug.Assert(fields is not null);

            _fields = fields;
            _length = length;
            _position = -1;
            Current = default!;
        }

        public TField Current { get; private set; }

        object? IEnumerator.Current => Current;

        public FieldEnumerator GetEnumerator()
        {
            var enumerator = this;
            enumerator.Reset();
            return enumerator;
        }

        IEnumerator<TField> IEnumerable<TField>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool MoveNext()
        {
            var length = _length;

            if (length == 0)
            {
                Current = default!;
                return false;
            }

            if (_position < _length && ++_position < _length)
            {
                Current = _fields[_position];
                return true;
            }

            Current = default!;
            return false;
        }

        public void Reset()
        {
            _position = -1;
            Current = default!;
        }

        void IDisposable.Dispose() { }
    }
}

file static class Partitioner
{
    public static void PartitionByAccessibility<T>(T[] array) where T : IFusionFieldDefinition
    {
        if (array.Length <= 1)
        {
            return;
        }

        var writeIndex = 0;

        // Move all accessible items to the front, preserving order
        for (var i = 0; i < array.Length; i++)
        {
            if (!array[i].IsInaccessible)
            {
                if (i != writeIndex)
                {
                    // Swap
                    var temp = array[writeIndex];
                    array[writeIndex] = array[i];
                    array[i] = temp;
                }
                writeIndex++;
            }
        }
    }
}
