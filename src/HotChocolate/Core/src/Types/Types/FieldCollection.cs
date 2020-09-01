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
        private readonly List<T> _fields;

        public FieldCollection(IEnumerable<T> fields)
        {
            if (fields is null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            _fields = fields is List<T> list ? list : fields.ToList();
            _fieldsLookup = _fields.ToDictionary(t => t.Name);
        }

        public T this[string fieldName] => _fieldsLookup[fieldName];

        public T this[int index] => _fields[index];

        public int Count => _fields.Count;

        public bool ContainsField(NameString fieldName) =>
            _fieldsLookup.ContainsKey(fieldName.EnsureNotEmpty(nameof(fieldName)));

        public bool TryGetField(NameString fieldName, [NotNullWhen(true)] out T? field) =>
            _fieldsLookup.TryGetValue(fieldName.EnsureNotEmpty(nameof(fieldName)), out field!);

        public IEnumerator<T> GetEnumerator()
        {
            return _fields.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static FieldCollection<T> Empty { get; } =
            new FieldCollection<T>(Enumerable.Empty<T>());
    }
}
