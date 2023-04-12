using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Fusion.Metadata;

internal sealed class ObjectFieldInfoCollection : IEnumerable<ObjectFieldInfo>
{
    private readonly Dictionary<string, ObjectFieldInfo> _fields;

    public ObjectFieldInfoCollection(IEnumerable<ObjectFieldInfo> fields)
    {
        _fields = fields.ToDictionary(t => t.Name, StringComparer.Ordinal);
    }

    public int Count => _fields.Count;

    public ObjectFieldInfo this[string fieldName] => _fields[fieldName];

    public bool TryGetField(string fieldName, [NotNullWhen(true)] out ObjectFieldInfo? value)
        => _fields.TryGetValue(fieldName, out value);

    public IEnumerator<ObjectFieldInfo> GetEnumerator() => _fields.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
