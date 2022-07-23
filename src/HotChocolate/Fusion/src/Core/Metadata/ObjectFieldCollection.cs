using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Fusion.Metadata;

internal sealed class ObjectFieldCollection : IEnumerable<ObjectField>
{
    private readonly Dictionary<string, ObjectField> _fields;

    public ObjectFieldCollection(IEnumerable<ObjectField> fields)
    {
        _fields = fields.ToDictionary(t => t.Name, StringComparer.Ordinal);
    }

    public int Count => _fields.Count;

    public ObjectField this[string fieldName] => _fields[fieldName];

    public bool TryGetValue(string fieldName, [NotNullWhen(true)] out ObjectField? value)
        => _fields.TryGetValue(fieldName, out value);

    public IEnumerator<ObjectField> GetEnumerator() => _fields.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
