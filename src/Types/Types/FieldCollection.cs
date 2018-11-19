using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Types
{
    public class FieldCollection<T>
        : IFieldCollection<T>
        where T : IField
    {
        private readonly Dictionary<string, T> _fields;

        public FieldCollection(IEnumerable<T> fields)
        {
            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            _fields = fields.ToDictionary(t => t.Name);
            Count = _fields.Count;
            IsEmpty = _fields.Count == 0;
        }

        public T this[NameString fieldName] => _fields[fieldName];

        public int Count { get; }

        public bool IsEmpty { get; }

        public bool ContainsField(NameString fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentException(
                    "A field name must at least consist of one letter.",
                    nameof(fieldName));
            }

            return _fields.ContainsKey(fieldName);
        }

        public bool TryGetField(NameString fieldName, out T field)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentException(
                    "A field name must at least consist of one letter.",
                    nameof(fieldName));
            }

            return _fields.TryGetValue(fieldName, out field);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _fields.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
