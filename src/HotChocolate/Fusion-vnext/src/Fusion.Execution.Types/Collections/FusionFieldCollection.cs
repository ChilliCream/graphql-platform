using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Collections;

public abstract class FusionFieldDefinitionCollection<TField>
    : IEnumerable<TField> where TField : IFieldDefinition
{
    private readonly FrozenDictionary<string, TField> _map;

    protected FusionFieldDefinitionCollection(TField[] fields)
    {
        _map = fields.ToFrozenDictionary(t => t.Name);
    }

    public int Count => _map.Count;

    public TField this[string name] => _map[name];

    public bool TryGetField(string name, [NotNullWhen(true)] out TField? field)
        => _map.TryGetValue(name, out field);

    public bool ContainsName(string name)
        => _map.ContainsKey(name);

    public IEnumerator<TField> GetEnumerator()
        => ((IEnumerable<TField>)_map.Values).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
