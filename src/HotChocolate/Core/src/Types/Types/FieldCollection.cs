using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;

namespace HotChocolate.Types
{
    public class FieldCollection<T>
        : IFieldCollection<T>
        where T : IField
    {
        private readonly Dictionary<NameString, T> _fieldsLookup;
        private readonly List<T> _fields;

        public FieldCollection(IEnumerable<T> fields)
        {
            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            _fields = fields.OrderBy(t => t.Name.Value, StringComparer.OrdinalIgnoreCase).ToList();
            _fieldsLookup = _fields.ToDictionary(t => t.Name);

            IsEmpty = _fields.Count == 0;
        }

        public T this[string fieldName] => _fieldsLookup[fieldName];

        public int Count => _fields.Count;

        public bool IsEmpty { get; }

        public bool ContainsField(NameString fieldName)
        {
            return _fieldsLookup.ContainsKey(
                fieldName.EnsureNotEmpty(nameof(fieldName)));
        }

        public bool TryGetField(NameString fieldName, out T field)
        {
            return _fieldsLookup.TryGetValue(
                fieldName.EnsureNotEmpty(nameof(fieldName)),
                out field);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _fields.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
