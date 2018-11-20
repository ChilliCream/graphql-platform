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
        private readonly Dictionary<NameString, T> _fields;

        public FieldCollection(IEnumerable<T> fields)
        {
            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            _fields = fields.ToDictionary(t => (NameString)t.Name);
            Count = _fields.Count;
            IsEmpty = _fields.Count == 0;
        }

        public T this[string fieldName] => _fields[fieldName];

        public int Count { get; }

        public bool IsEmpty { get; }

        public bool ContainsField(NameString fieldName)
        {
            return _fields.ContainsKey(
                fieldName.EnsureNotEmpty(nameof(fieldName)));
        }

        public bool TryGetField(NameString fieldName, out T field)
        {
            return _fields.TryGetValue(
                fieldName.EnsureNotEmpty(nameof(fieldName)),
                out field);
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
