using System.Collections;
using System.Collections.Frozen;

namespace HotChocolate.Fusion.Types.Collections;

public abstract class CompositeFieldCollection<TField> : IEnumerable<TField> where TField : ICompositeField
{
    private readonly FrozenDictionary<string, TField> _fields;

    protected CompositeFieldCollection(IEnumerable<TField> fields)
    {
        _fields = fields.ToFrozenDictionary(t => t.Name);
    }

    public int Count => _fields.Count;

    public TField this[string name] => _fields[name];

    public IEnumerator<TField> GetEnumerator()
        => ((IEnumerable<TField>)_fields.Values).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
