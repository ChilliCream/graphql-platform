using System.Collections;
using System.Collections.Frozen;

namespace HotChocolate.Fusion.Planning.Collections;

public class SourceFieldCollection<TField> : IEnumerable<TField> where TField : ISourceField
{
    private readonly FrozenDictionary<string, TField> _fields;

    protected SourceFieldCollection(IEnumerable<TField> fields)
    {
        _fields = fields.ToFrozenDictionary(t => t.SchemaName);
    }

    public int Count => _fields.Count;

    public TField this[string schemaName] => _fields[schemaName];

    public IEnumerator<TField> GetEnumerator()
        => ((IEnumerable<TField>)_fields.Values).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
