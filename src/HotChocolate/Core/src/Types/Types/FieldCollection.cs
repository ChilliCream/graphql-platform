using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

#nullable enable

namespace HotChocolate.Types
{
    public class FieldCollection<T>
        : IFieldCollection<T>
        where T : class, IField
    {
        private readonly Dictionary<NameString, T> _fieldsLookup;
        private readonly T[] _fields;

        public FieldCollection(IEnumerable<T> fields, bool sortByName = false)
        {
            if (fields is null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            _fields = sortByName
                ? fields.OrderBy(t => t.Name).ToArray()
                : fields is T[] array ? array : fields.ToArray();

            _fieldsLookup = new Dictionary<NameString, T>(_fields.Length);

            foreach (var field in _fields)
            {
                _fieldsLookup.Add(field.Name, field);
            }
        }

        public T this[string fieldName] => _fieldsLookup[fieldName];

        public T this[int index] => _fields[index];

        public int Count => _fields.Length;

        [Obsolete("This method will be removed soon.")]
        public int IndexOfField(NameString fieldName)
        {
            for (var i = 0; i < _fields.Length; i++)
            {
                if (fieldName.Equals(_fields[i].Name))
                {
                    return i;
                }
            }

            return -1;
        }

        public bool ContainsField(NameString fieldName) =>
            _fieldsLookup.ContainsKey(fieldName.EnsureNotEmpty(nameof(fieldName)));

        public bool TryGetField(NameString fieldName, [NotNullWhen(true)] out T? field)
        {
            if (_fieldsLookup.TryGetValue(
                fieldName.EnsureNotEmpty(nameof(fieldName)),
                out var item))
            {
                field = item;
                return true;
            }

            field = default;
            return false;
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var field in _fields)
            {
                yield return field;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public static FieldCollection<T> From(IEnumerable<T> fields, bool sortByName = false)
        {
            if (fields.Any())
            {
                return new FieldCollection<T>(fields, sortByName);
            }

            return Empty;
        }

        public static FieldCollection<T> Empty { get; } = new(Array.Empty<T>());
    }
}
