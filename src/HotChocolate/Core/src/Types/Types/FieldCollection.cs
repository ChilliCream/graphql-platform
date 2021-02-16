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
        private readonly Dictionary<NameString, (int Index, T Field)> _fieldsLookup =
            new Dictionary<NameString, (int Index, T Field)>();

        private readonly List<T> _fields;

        public FieldCollection(IEnumerable<T> fields, bool sortByName = false)
        {
            if (fields is null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            _fields = sortByName
                ? fields.OrderBy(t => t.Name).ToList()
                : fields is List<T> list ? list : fields.ToList();

            for (var i = 0; i < _fields.Count; i++)
            {
                T field = _fields[i];
                _fieldsLookup.Add(field.Name, (i, field));
            }
        }

        public T this[string fieldName] => _fieldsLookup[fieldName].Field;

        public T this[int index] => _fields[index];

        public int Count => _fields.Count;

        public int IndexOfField(NameString fieldName)
        {
            return _fieldsLookup.TryGetValue(fieldName, out (int Index, T Field) item)
                ? item.Index
                : -1;
        }

        public bool ContainsField(NameString fieldName) =>
            _fieldsLookup.ContainsKey(fieldName.EnsureNotEmpty(nameof(fieldName)));

        public bool TryGetField(NameString fieldName, [NotNullWhen(true)] out T? field)
        {
            if (_fieldsLookup.TryGetValue(
                fieldName.EnsureNotEmpty(nameof(fieldName)),
                out (int Index, T Field) item))
            {
                field = item.Field;
                return true;
            }

            field = default;
            return false;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _fields.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static FieldCollection<T> From(IEnumerable<T> fields, bool sortByName = false)
        {
            if (fields.Any())
            {
                return new FieldCollection<T>(fields, sortByName);
            }

            return Empty;
        }

        public static FieldCollection<T> Empty { get; } =
            new FieldCollection<T>(Enumerable.Empty<T>(), false);
    }
}
